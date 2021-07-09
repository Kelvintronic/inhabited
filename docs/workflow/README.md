## Workflow Details
This workflow folder is a place to organise and structure the work that needs to be done.

We put the biggest ideas in ***epics***. These are broad sweeping statements that would need to be broken into smaller ***stories***.

Just add epics and stories to the relevant file. They are fluid and can be moved, discarded, split up, merged, whatever. The **stories** deemed most desirable should be moved into the backlog.

### stories.md
Think of **stories** as placeholders for deeper conversations around implementation details and system architecture.
These thin vertical slices through the system provide a concise, readable statement of value (however measured in the context).

User stories are specified by using the following template:  
*`"As a [user role], I want to [verb-centric behaviour], so that [user value added]."`*

The square brackets denote parameterisation that distinguishes one user story from another. A concrete example should illuminate further:  
*`"As an unauthenticated but registered user, I want to reset my password, so that I can log on to the system if I forget my password."`*

There are many things to note about this user story. First of all, *there is not nearly enough detail to actually implement the behaviour required*. Note that the user story is written from the perspective of a *user*. Often stories are wrongly written from the perspective of developers. This is why the first part of the template &mdash; *As a \[user role\]* &mdash; is so important. Similarly, the *\[user value added\]* portion is just as important because, without this, it is easy to lose sight of the underlying reason that the user story exists. This is usually what ties the user story to its parent epic; the example just given could belong to an epic such as *&quot;Forgotten user credentials are recoverable.&quot;* This story would probably be grouped with the story in which the user has forgotten his or her logon name and the story in which the user has forgotten both logon name and password.

### backlog.md
**Stories** chosen for implementation are moved into the backlog, which is always ordered by priority. Consider this the current most valuable features to be implemented.

### wip.md
Contains **stories** from the backlog that are in active development. Self-explanatory.

### done.md
**Stories** that have passed their acceptance test (*whatever that may be - [definition of done]*)
