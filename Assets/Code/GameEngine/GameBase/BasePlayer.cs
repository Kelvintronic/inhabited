using System.Collections;
using System.Collections.Generic;

namespace GameEngine
{
    public class PlayerBagSlot
    {
        public PlayerBagItem type;
        public int count;
    }

    public abstract class BasePlayer
    {
        public readonly string Name;
        public const byte MaxHealth = 100;
        
        private byte MaxBagSize = 5;
        private byte MaxBagSlotItemCount = 5;
        private float _speed = 4f;

        // cooldown timers
        private GameTimer _shootTimer = new GameTimer(0.3f);
        protected GameTimer _activateTimer = new GameTimer(0.5f);

        protected WorldVector _position;
        protected float _rotation;
        protected uint _score;
        protected uint _cash;
        protected byte _health = MaxHealth;
        protected bool _active;
        protected List<PlayerBagSlot> _bag;

        public const float Radius = 0.5f;
        public uint Score => _score;
        public uint Cash => _cash;
        public bool IsAlive => _health > 0;
        public IReadOnlyList<PlayerBagSlot> Bag => _bag.AsReadOnly();

        public bool IsBagFull => Bag.Count == MaxBagSize;
        public byte Health
        {
            get => _health;
            set
            {
                if (value >= 0 && value <= MaxHealth)
                {
                    _health = value;
                }
            }
        }
        public float HealthNormalised => (float)_health / MaxHealth;

        public bool IsActive => _active;
        public float Speed => _speed;
        public WorldVector Position => _position;
        public float Rotation => _rotation;
        public readonly byte Id;

        public int player;
        public int Ping;

        protected PlayerInputPacket _nextCommand;        // only used by ClientPlayer

        protected BasePlayer(string name, byte id)
        {
            Id = id;
            Name = name;
            _bag = new List<PlayerBagSlot>();
        }

        public virtual void Spawn(float x, float y)
        {
            _position = new WorldVector(x, y);
            _active = true;
        }

        public virtual void SetActive(bool active)
        {
            _active = active;
        }

        public void AddScore(uint points)
        {
            _score += points;
        }

        public void AddCash()
        {
            _cash++;
        }

        public bool SubtractCash(byte amount)
        {
            if(_cash-amount>=0)
            {
                _cash -= amount;
                return true;
            }
            return false;
        }

        public void AddHealth(byte amount)
        {
            if (_health + amount > MaxHealth)
            {
                _health = MaxHealth;
            }
            else
            {
                _health += amount;
            }
        }

        public void SubtractHealth(byte amount)
        {
            if (amount > _health)
            {
                _health = 0;
            }
            else
            {
                _health -= amount;
            }                
        }

        public bool AddBagItem(PlayerBagItem item)
        {
            int index = _bag.FindIndex(o => o.type == item);
            if(index==-1)
            {
                if (_bag.Count == MaxBagSize)
                    return false;
                else
                {
                    _bag.Add(new PlayerBagSlot
                    {
                        type = item,
                        count = 1
                    });
                    return true;
                }
            }
            if (_bag[index].count == MaxBagSlotItemCount)
                return false;
            _bag[index].count++;
            return true;
        }

        public string GetBagContentString()
        {
            string bagitems = "";
            foreach(var item in _bag)
            {
                switch (item.type)
                {
                    case PlayerBagItem.Bomb:
                        bagitems = bagitems + "B ";
                        break;
                    case PlayerBagItem.KeyBlue:
                        bagitems = bagitems + "Kb ";
                        break;
                    case PlayerBagItem.KeyRed:
                        bagitems = bagitems + "Kr ";
                        break;
                    case PlayerBagItem.KeyGreen:
                        bagitems = bagitems + "Kg ";
                        break;
                }
            }
            return bagitems;
        }

        // Called by the server and client when a new level begins
        // use this to remove level specific items from the bag
        // or reset health or things like that
        public void NewLevelReset()
        {
            // wake up player
            _active = true;

            // remove all keys
            _bag.RemoveAll(item => item.type == PlayerBagItem.KeyBlue);
            _bag.RemoveAll(item => item.type == PlayerBagItem.KeyRed);
            _bag.RemoveAll(item => item.type == PlayerBagItem.KeyGreen);
        }

        /*  private void Shoot()
          {
              WorldVector dir = new WorldVector(MathFloat.Cos(_rotation),MathFloat.Sin(_rotation));
              _playerManager.OnShoot(this, dir); 
          }*/

        public bool ApplyShoot()
        {
            if (_shootTimer.IsTimeElapsed)
            {
                _shootTimer.Reset();
                return true;
            }
            return false;
        }

        public virtual void ApplyInput(PlayerInputPacket command, float delta)
        {

        }

        public virtual void Update(float delta) 
        {
            // tick over cooldown timers
            _shootTimer.UpdateAsCooldown(delta);
            _activateTimer.UpdateAsCooldown(delta);
        }

        public bool ApplyPickup(WorldObject worldObject)
        {
            // cash doesn't take up a slot
            if (worldObject.Type == ObjectType.Cash)
            {
                AddCash();
                return true;
            }
            switch (worldObject.Type)
            {
                case ObjectType.KeyRed:
                  /*  if (_bag.Exists(item => item == PlayerBagItem.KeyRed))
                        return false;*/
                    return AddBagItem(PlayerBagItem.KeyRed);
                    //return true;
                case ObjectType.KeyBlue:
                  /*  if (_bag.Exists(item => item == PlayerBagItem.KeyBlue))
                        return false;*/
                    return AddBagItem(PlayerBagItem.KeyBlue);
                case ObjectType.KeyGreen:
                 /*   if (_bag.Exists(item => item == PlayerBagItem.KeyGreen))
                        return false;*/
                    return AddBagItem(PlayerBagItem.KeyGreen);
                case ObjectType.Bomb:
                    return AddBagItem(PlayerBagItem.Bomb);
                case ObjectType.Health:
                    return AddBagItem(PlayerBagItem.Health);
            }
            return false;
        }

        public PlayerBagItem ApplyUseBagItem(int slot,bool drop)
        {
            if (slot > _bag.Count || _bag.Count == 0)
                return PlayerBagItem.Lint;

            var item = _bag[slot];

            if(item.count>0)
            // dispose of any dropped item or item consumed (i.e. not keys)
            if(!drop)
                switch(item.type)
                {
                    case PlayerBagItem.Health:
                        AddHealth(50);
                        break;
                }

            item.count--;
            if (item.count == 0)
                _bag.RemoveAt(slot);
            return item.type;
        }

        public bool RemoveBagItem(PlayerBagItem type)
        {
            int index = _bag.FindIndex(i => i.type == type);
            if(index!=-1)
            {
                var slot = _bag[index];
                slot.count--;
                if (slot.count == 0)
                    _bag.RemoveAt(index);
                return true;
            }
            return false;
        }

        public WorldVector GetLookVector()
        {
            return new WorldVector(MathFloat.Cos(_rotation), MathFloat.Sin(_rotation));
        }

    }
}

