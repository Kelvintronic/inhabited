using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Assertions;

namespace GameEngine.Search
{
    /// <summary>
    /// Class <c>AStarSearch</c> finds optimal paths in a directed graph
    /// </summary>
    public class AStarSearch : MonoBehaviour
    {
        /// <summary>
        /// Explore up to this many candidate paths in any given frame
        /// </summary>
        private const uint PathsPerFrame = 100;
        /// <summary>
        /// Reject incoming search jobs if this many are already queued
        /// </summary>
        private const uint MaxPendingJobs = 500;
        /// <summary>
        /// Discard candidate paths costing more than this
        /// </summary>
        private const float MaxPathCost = 30f;
        private readonly (string action, int dx, int dy, double cost)[] actions =
            new (string, int, int, double)[] {
                ("N", 0, 1, 1),
                ("NE", 1, 1, 1.1),
                ("E", 1, 0, 1),
                ("SE", 1, -1, 1.1),
                ("S", 0, -1, 1),
                ("SW", -1, -1, 1.1),
                ("W", -1, 0, 1),
                ("NW", -1, 1, 1.1) };

     //   private ServerLogic _serverLogic;
        private GameTimer _inputCooldown = new GameTimer(0.2f);

        private Queue<ISearchJob> _pendingJobs;
        private ISearchJob _currentJob;
        private List<ISearchJob> _finishedJobs;
        private AStarFrontier _frontier;

        private MapArray _map;
        private Coroutine _mainCoroutine;

        public int FinishedJobCount => _finishedJobs.Count;
        public int QueuedJobCount => _pendingJobs.Count;

        /// <summary>
        /// Adds a job to the queue for processing
        /// </summary>
        /// <param name="job"></param>
        /// <returns>number of currently pending <c>ISearchJob</c>s, -1 if job rejected</returns>
        public int AddJob(ISearchJob job)
        {
            Debug.Log("Job added");
            if (_pendingJobs.Count > MaxPendingJobs)
            {
                Debug.LogWarning("Rejecting search job due to congestion");
                return -1;
            }
            else
            {
                _pendingJobs.Enqueue(job);
                return _pendingJobs.Count;
            }
        }

        public IReadOnlyList<ISearchJob> FinishedJobs
        {
            get => _finishedJobs.AsReadOnly();
        }

        /// <summary>
        /// Returns (via a generator) a sequence of <c>Arc</c> objects corresponding to all the edges
        /// in which the given node is the tail node
        /// </summary>
        /// <param name="node">the tail node to search from</param>
        public IEnumerable<Arc> OutgoingArcs(Vector2Int node)
        {
            if(_map==null)
            {
                Debug.LogWarning("Attempt to use OutgoingArcs() before AStarSearch initialisation");
                yield break;
            }

            foreach (var action in actions)
            {
                // confirm array bounds and check each move
                var (x, y) = (node.x + action.dx, node.y + action.dy);

                if (x >= 0 && x < _map.xCount)
                {
                    if (y >= 0 && y < _map.yCount)
                    {
                        // experimenting with ignorning certain ObjectTypes
                        switch (_map.Array[x, y].type)
                        {
                            // ignore cases
                            case ObjectType.None:
                            case ObjectType.NPC_Intent:
                            case ObjectType.NPCBug:
                            case ObjectType.NPCMercenary:
                            case ObjectType.NPCTrader:
                                // valid move found, yield the edge
                                yield return new Arc(node, new Vector2Int(x, y), action.action, action.cost);
                                break;
                            // permanent cases
                            case ObjectType.Wall:
                                continue;
                            // transient cases (everything else)
                            default:
                                // invalid move due to transient obstacle allow request to proceed with notification
                                yield return new Arc(node, new Vector2Int(x, y), action.action, action.cost,true);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Coroutine to search for an optimal <c>Path</c> for the given <c>SearchJob</c>.
        /// Processes one <c>FrontierPath</c> each iteration.
        /// Solution (if found) will be stored in the job
        /// </summary>
        /// <param name="job">details of the search</param>
        public IEnumerator FindSolution(ISearchJob job)
        {
            _frontier = new AStarFrontier(job.StartNode, job.EstimatedCostToGoal(job.StartNode));
            var pathsExplored = 0;
           
            foreach (var path in _frontier.Paths())
            {
                if (job.IsCancelled)
                {
                    break;
                }

                if (path.Cost > MaxPathCost)
                {
                    Debug.LogWarning($"Candidate path cost ({path.Cost}) exceeds MaxPathCost ({MaxPathCost}); discarding");
                    continue;
                }

                var nodeToExpand = path.End().head;
                if (job.IsGoal(nodeToExpand))
                {
                    job.HasSolution = true;
                    job.Solution = path;
                    break;
                }

                foreach (var arc in OutgoingArcs(nodeToExpand))
                {
                    if(!arc.isTransientObstacle)
                    {
                        // if arc is a genuine path step
                        // create new frontier
                        var newPath = path.Clone();
                        newPath.Add(arc);
                        _frontier.Add(newPath, job.EstimatedCostToGoal(arc.head));
                    }
                    else
                    {

                        // otherwise add reference to transient
                        // and allow path to end
                        job.AddTransient(arc.head);
                    }
                }

                if (!(pathsExplored++ < PathsPerFrame))
                {
                    pathsExplored = 0;
                    yield return null;
                }
            }

            job.IsFinished = true;
        }

        private void Awake()
        {
            // internal initialisation
            _pendingJobs = new Queue<ISearchJob>();
            _finishedJobs = new List<ISearchJob>();
            _map = null;
        }

        // Start is called before the first frame update
        private void Start()
        {
            // initialisation involving external dependencies
     /*       _serverLogic = GameObject.FindObjectOfType<ServerLogic>();

            if (!(_serverLogic != null && _serverLogic.IsStarted))
            {
                // this is not the server, so just exit without doing anything
                Debug.Log("Because this is the client, AStarSearch system will not be activated");
                return;
            }

            // start coroutine for SearchJob processing
            StartCoroutine(ProcessSearchJobs());*/
        }

        public void Initialise(MapArray map)
        {
            Stop();
            _map = map;
            StartCoroutine(ProcessSearchJobs());
        }

        public void Stop()
        {
            StopAllCoroutines();
            _pendingJobs.Clear();
            _finishedJobs.Clear();
            _map = null;
        }

        private void Update()
        {

/*            if(_map!=null)
                Debug.Log("Queued jobs = " + _pendingJobs.Count + " Finished Jobs = " + FinishedJobs.Count);*/

            _inputCooldown.UpdateAsCooldown(Time.deltaTime);

            if (!_inputCooldown.IsTimeElapsed)
            {
                return;
            }

            // check for J key to manually create a job
       /*     if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                AddJob(new AStarSearchJob(new Vector2Int(6, 4), new Vector2Int(7, 8)));
                _inputCooldown.Reset();
            }*/
        }

        private IEnumerator ProcessSearchJobs()
        {
            while (true)
            {
                yield return new WaitWhile(() => _pendingJobs.Count == 0);

                _currentJob = _pendingJobs.Dequeue();
                yield return StartCoroutine(FindSolution(_currentJob));

                Assert.IsTrue(_currentJob.IsFinished);
                if (_currentJob.IsCancelled)
                {
                    // Debug.Log("Search job was cancelled. Discarding.");
                    _currentJob = null;
                }
                else
                {
                  //  Debug.Log(_currentJob.ToString());
                    _finishedJobs.Add(_currentJob);
                }
            }
        }

        private IEnumerator AddJobLater(float seconds, ISearchJob job)
        {
            yield return new WaitForSeconds(seconds);
            AddJob(job);
        }
    }
}