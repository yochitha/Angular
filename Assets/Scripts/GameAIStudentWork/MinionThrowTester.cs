using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using GameAI;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(MinionScript))]
public class MinionThrowTester : MonoBehaviour
{
    public const string StudentName = "Bob the Minion";

    public const string CollectBallStateName = "CollectBall";
    public const string ThrowBallStateName = "ThrowBall";



    // For throws...
    public static float MaxAllowedThrowPositionError = (0.25f + 0.5f)*0.99f;

    // Data that each FSM state gets initialized with (passed as init param)
    FiniteStateMachine<MinionFSMData> fsm;

    public MinionScript Minion { get; private set; }

    PrisonDodgeballManager Mgr;
    public TeamShare TeamData { get; private set; }

    struct MinionFSMData
    {
        public MinionThrowTester MinionFSM { get; private set; }
        public MinionScript Minion { get; private set; }
        public PrisonDodgeballManager Mgr { get; private set; }
        public PrisonDodgeballManager.Team Team { get; private set; }
        public TeamShare TeamData { get; private set; }

        public MinionFSMData(
            MinionThrowTester minionFSM,
            MinionScript minion,
            PrisonDodgeballManager mgr,
            PrisonDodgeballManager.Team team,
            TeamShare teamData
            )
        {
            MinionFSM = minionFSM;
            Minion = minion;
            Mgr = mgr;
            Team = team;
            TeamData = teamData;
        }
    }


    //// Turn to face target, but taking into account hand offset
    //public static Vector3 FindTurnTowardsTargetForAim(Vector3 targetPos, Vector3 minionPos, Vector3 minionForward, float holdSpotOffset)
    //{
    //    var targetPos2d = new Vector2(targetPos.x, targetPos.z);
    //    var minionPos2d = new Vector2(minionPos.x, minionPos.z);

    //    var minionToTarget2d = targetPos2d - minionPos2d;

    //    float distMinionFromTarget = minionToTarget2d.magnitude;

    //    var angle = Mathf.Atan2(holdSpotOffset, distMinionFromTarget);

    //    //Debug.Log($"Rotating by {angle * Mathf.Rad2Deg} degrees");

    //    float sin = Mathf.Sin(angle);
    //    float cos = Mathf.Cos(angle);

    //    // Rotate
    //    var newMinionToTarget2d = new Vector2(
    //        minionToTarget2d.x * cos - minionToTarget2d.y * sin,
    //        minionToTarget2d.x * sin + minionToTarget2d.y * cos);

    //    return new Vector3(minionPos2d.x + newMinionToTarget2d.x, targetPos.y, minionPos2d.y + newMinionToTarget2d.y);
    //}


    // Note: You have to implement the following method with prediction:
    // Either directly solved (e.g. Law of Cosines or similar) or iterative.
    // You cannot modify the method signature. However, if you want to do more advanced
    // prediction (such as analysis of the navmesh) then you can make another method that calls
    // this one. 
    // Be sure to run the editor mode unit test to confirm that this method runs without
    // any gamemode-only logic
    public static bool PredictThrow(
        // The initial launch position of the projectile
        Vector3 projectilePos,
        // The initial ballistic speed of the projectile
        float maxProjectileSpeed,
        // The gravity vector affecting the projectile (likely passed as Physics.gravity)
        Vector3 projectileGravity,
        // The initial position of the target
        Vector3 targetInitPos,
        // The constant velocity of the target (zero acceleration assumed)
        Vector3 targetConstVel,
        // The forward facing direction of the target. Possibly of use if the target
        // velocity is zero
        Vector3 targetForwardDir,
        // For algorithms that approximate the solution, this sets a limit for how far
        // the target and projectile can be from each other at the interceptT time
        // and still count as a successful prediction
        float maxAllowedErrorDist,
        // Output param: The solved projectileDir for ballistic trajectory that intercepts target
        out Vector3 projectileDir,
        // Output param: The speed the projectile is launched at in projectileDir such that
        // there is a collision with target. projectileSpeed must be <= maxProjectileSpeed
        out float projectileSpeed,
        // Output param: The time at which the projectile and target collide
        out float interceptT,
        // Output param: An alternate time at which the projectile and target collide
        // Note that this is optional to use and does NOT coincide with the solved projectileDir
        // and projectileSpeed. It is possibly useful to pass on to an incremental solver.
        // It only exists to simplify compatibility with the ShootingRange
        out float altT)
    {
        // TODO implement an accurate throw with prediction. This is just a placeholder

        // FYI, if Minion.transform.position is sent via param targetPos,
        // be aware that this is the midpoint of Minion's capsuleCollider
        // (Might not be true of other agents in Unity though. Just keep in mind for future game dev)

        // Only going 2D for simple demo. this is not useful for proper prediction
        // Basically, avoiding throwing down at enemies since we aren't predicting accurately here.
        var targetPos2d = new Vector3(targetInitPos.x, 0f, targetInitPos.z);
        var launchPos2d = new Vector3(projectilePos.x, 0f, projectilePos.z);

        var relVec = (targetPos2d - launchPos2d);
        interceptT = relVec.magnitude / maxProjectileSpeed;
        altT = -1f;

        // This is a hard-coded approximate sort of of method to figure out a loft angle
        // This is NOT the right thing to do for your prediction code!
        var normAngle = Mathf.Lerp(0f, 20f, interceptT * 0.007f);
        var v = Vector3.Slerp(relVec.normalized, Vector3.up, normAngle);

        // Make sure this is normalized! (The direction of your throw)
        projectileDir = v;

        // You'll probably want to leave this as is. For advanced prediction you can slow your throw down
        // You don't need to predict the speed of your throw. Only the direction assuming full speed
        projectileSpeed = maxProjectileSpeed;

        // TODO return true or false based on whether target can actually be hit
        // This implementation just thinks, "I guess so?", and returns true
        // Implementations that don't exactly solve intercepts will need to test the approximate
        // solution with maxAllowedErrorDist. If your solution does solve exactly, you will
        // probably want to add a debug assertion to check your solution against it.
        return true;

    }



    // Simple demo of shared info amongst the team
    // You can modify this as necessary for advanced team strategy
    // Tracking teammates is added to get you started
    public class TeamShare
    {
        public MinionScript[] TeamMates { get; private set; }
        public int TeamSize { get; private set; }
        int currTeamMateRegSpot = 0;

        public TeamShare(int teamSize)
        {
            TeamSize = teamSize;
            TeamMates = new MinionScript[TeamSize];
        }

        public void AddTeamMember(MinionScript m)
        {
            TeamMates[currTeamMateRegSpot] = m;
            ++currTeamMateRegSpot;
        }

        public bool TeamMemberCanBeRescued(out MinionScript firstHelplessMinion)
        {
            firstHelplessMinion = null;

            foreach (var m in TeamMates)
            {
                if (m == null)
                    continue;

                if (m.CanBeRescued)
                {
                    firstHelplessMinion = m;
                    return true;
                }
            }
            return false;
        }
    }

    // Create a base class for our states to have access to the parent MinionThrowTester, and other info
    // This class can be modified!
    abstract class MinionStateBase
    {
        public virtual string Name => throw new System.NotImplementedException();

        protected IFiniteStateMachine<MinionFSMData> ParentFSM;
        protected MinionThrowTester MinionFSM;
        protected MinionScript Minion;
        protected PrisonDodgeballManager Mgr;
        protected PrisonDodgeballManager.Team Team;
        protected TeamShare TeamData;
        protected PrisonDodgeballManager.DodgeballInfo[] dbInfo;

        public virtual void Init(IFiniteStateMachine<MinionFSMData> parentFSM,
            MinionFSMData minFSMData)
        {
            ParentFSM = parentFSM;
            MinionFSM = minFSMData.MinionFSM;
            Minion = minFSMData.Minion;
            Mgr = minFSMData.Mgr;
            Team = minFSMData.Team;
            TeamData = minFSMData.TeamData;
        }

        // Note: You can add extra methods here that you want to be available to all states

        // determineRegion is an expensive operation to determine whether the minion
        // can go to the dodgeball. Don't ask for it if you don't need it
        protected bool UpdateAllDodgeballInfo(bool determineRegion)
        {
            if (dbInfo == null || dbInfo.Length != Mgr.TotalBalls)
                dbInfo = new PrisonDodgeballManager.DodgeballInfo[Mgr.TotalBalls];

            return Mgr.GetAllDodgeballInfo(Minion.Team, ref dbInfo, determineRegion);
        }

        protected bool FindClosestAvailableDodgeball(out PrisonDodgeballManager.DodgeballInfo dodgeballInfo)
        {

            var dist = float.MaxValue;
            bool found = false;

            dodgeballInfo = default;

            foreach (var db in dbInfo)
            {
                if (!db.IsHeld && db.State == PrisonDodgeballManager.DodgeballState.Neutral && db.Reachable)
                {
                    var d = Vector3.Distance(db.Pos, Minion.transform.position);

                    if (d < dist)
                    {
                        found = true;
                        dist = d;
                        dodgeballInfo = db;
                    }

                }
            }

            return found;
        }

        protected void InternalEnter()
        {
            MinionFSM.Minion.DisplayText(Name);
        }

        // globalTransition parameter is to notify if transition was triggered
        // by a global transition (wildcard)
        public virtual void Exit(bool globalTransition) { }
        public virtual void Exit() { Exit(false); }

        public virtual DeferredStateTransitionBase<MinionFSMData> Update()
        {
            return null;
        }

    }

    // Create a base class for our states to have access to the parent MinionThrowTester, and other info
    abstract class MinionState : MinionStateBase, IState<MinionFSMData>
    {
        public virtual void Enter() { InternalEnter(); }
    }

    // Create a base class for our states to have access to the parent MinionThrowTester, and other info
    abstract class MinionState<S0> : MinionStateBase, IState<MinionFSMData, S0>
    {
        public virtual void Enter(S0 s) { InternalEnter(); }
    }

    // Create a base class for our states to have access to the parent MinionThrowTester, and other info
    abstract class MinionState<S0, S1>: MinionStateBase, IState<MinionFSMData, S0, S1>
    {
        public virtual void Enter(S0 s0, S1 s1) { InternalEnter(); }
    }

    // If you need MinionState<>s with more parameters (up to four total), you can add them following the pattern above




    // Go get a ball!
    class CollectBallState : MinionState
    {
        public override string Name => CollectBallStateName;

        DeferredStateTransition<MinionFSMData> ThrowTransition;
 

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            ThrowTransition = ParentFSM.CreateStateTransition(ThrowBallStateName);
        }

        public override void Enter()
        {
            base.Enter();

            Minion.GoTo(Mgr.TeamCenter(Team).position);

        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            // The dodgeballs come to us like killer tomatoes or The Prisoner weather balloon or something...
            if (Minion.HasBall)
                return ThrowTransition;

            Minion.GoTo(Mgr.TeamCenter(Team).position);

            return ret;
        }
    }





    // Throw the ball at the enemy
    class ThrowBallState : MinionState
    {
        public override string Name => ThrowBallStateName;

        int opponentIndex = -1;
        PrisonDodgeballManager.OpponentInfo opponentInfo;
        bool hasOpponent = false;

        DeferredStateTransition<MinionFSMData> CollectBallTransition;

        public override void Init(IFiniteStateMachine<MinionFSMData> parentFSM, MinionFSMData minFSMData)
        {
            base.Init(parentFSM, minFSMData);

            // create deferred transitions in advanced and reuse them to avoid garbage collection hit during game
            CollectBallTransition = ParentFSM.CreateStateTransition(CollectBallStateName);
 
        }


        public override void Enter()
        {
            base.Enter();


            if (Mgr.FindClosestNonPrisonerOpponentIndex(Minion.transform.position, Team, out opponentIndex))
            {
                if (hasOpponent = Mgr.GetOpponentInfo(Team, opponentIndex, out opponentInfo))
                {
                    Minion.FaceTowards(opponentInfo.Pos);
                }
            }
        }

        public override void Exit(bool globalTransition)
        {

        }

        public override DeferredStateTransitionBase<MinionFSMData> Update()
        {
            DeferredStateTransitionBase<MinionFSMData> ret = null;

            // just in case something bad happened
            if (!Minion.HasBall)
            {
                return CollectBallTransition;
            }

            // Check if opponent still valid
            if ((hasOpponent = Mgr.GetOpponentInfo(Team, opponentIndex, out opponentInfo)) &&
                !opponentInfo.IsPrisoner && !opponentInfo.IsFreedPrisoner)
            {
                //Minion.FaceTowards(opponentInfo.Pos);
            }
            else
            {
                if (Mgr.FindClosestNonPrisonerOpponentIndex(Minion.transform.position, Team, out opponentIndex))
                {
                    if (hasOpponent = Mgr.GetOpponentInfo(Team, opponentIndex, out opponentInfo))
                    {
                        //Minion.FaceTowards(opponentInfo.Pos);
                    }
                }
            }

            // Nothing to do without opponent...
            if (!hasOpponent)
                return CollectBallTransition;



            var canThrow = PredictThrow(Minion.HeldBallPosition, Minion.ThrowSpeed, Physics.gravity, 
                    opponentInfo.Pos, opponentInfo.Vel, opponentInfo.Forward, MaxAllowedThrowPositionError,
                    out var univVDir, out var speedScalar, out var interceptT, out var altT);

          
            var intercept = Minion.HeldBallPosition + univVDir * speedScalar * interceptT;
            Minion.FaceTowardsForThrow(intercept);

            if (canThrow)
            {
                var speedNorm = speedScalar / Minion.ThrowSpeed;

                if (Minion.ThrowBall(univVDir, speedNorm))
                    ret = CollectBallTransition;
            }

            //if (canThrow)
            //{
            //    var intercept = Minion.HeldBallPosition + univVDir * speedScalar * interceptT;

            //    var holdOffsetDist = -0.622f;

            //    var adjIntercept = FindTurnTowardsTargetForAim(intercept, Minion.transform.position, Minion.transform.forward, holdOffsetDist);

            //    var angle = Minion.AbsAngleWith(adjIntercept);

            //    if (angle < Minion.MaxAllowedOffAngleThrow)
            //    {
            //        var speedNorm = speedScalar / Minion.ThrowSpeed;

            //        if (Minion.ThrowBall(univVDir, speedNorm))
            //            ret = CollectBallTransition;
            //    }
            //    else
            //    {
            //        Minion.FaceTowards(adjIntercept);

            //        // The following useful for visualizing aim towards target
            //        var newTargPos = intercept;

            //        var targetFacingDir = newTargPos - Minion.transform.position;

            //        var minionToNewIntercept2d = new Vector2(targetFacingDir.x, targetFacingDir.z);


            //        var newHoldPos = Minion.transform.position + Vector3.Cross(Vector3.up, targetFacingDir).normalized * holdOffsetDist;

            //        var holdToIntercept2d = new Vector2(intercept.x - newHoldPos.x, intercept.z - newHoldPos.z);

            //        Debug.DrawLine(newHoldPos, intercept, Color.magenta);
            //        Debug.DrawLine(Minion.transform.position, newTargPos, Color.white);
            //        Debug.DrawRay(Minion.transform.position, Minion.transform.forward, Color.green);

            //        //var checkAngle = Vector2.Angle(holdToIntercept2d, minionToNewIntercept2d);

            //        //if(checkAngle > 1f) 
            //        //    Debug.LogWarning($"Expected parallel: {checkAngle}");

            //    }
            //}

            return ret;
        }
    }



    private void Awake()
    {
        Minion = GetComponent<MinionScript>();

        if (Minion == null)
            Debug.LogError("No minion script");
    }


    protected void InitTeamData()
    {
        Mgr.SetTeamText(Minion.Team, StudentName);

        var o = Mgr.GetTeamDataShare(Minion.Team);

        if (o == null)
        {
            //Debug.Log($"Team Size: {Mgr.TeamSize}");
            TeamData = new TeamShare(Mgr.TeamSize);
            Mgr.SetTeamDataShare(Minion.Team, TeamData);
        }
        else
        {
            TeamData = o as TeamShare;

            if (TeamData == null)
                Debug.LogError("TeamData is null!");
        }

        TeamData.AddTeamMember(Minion);
    }


    // Start is called before the first frame update
    protected void Start()
    {

        Mgr = PrisonDodgeballManager.Instance;

        InitTeamData();

        var minionFSMData = new MinionFSMData(this, Minion, Mgr, Minion.Team, TeamData);

        fsm = new FiniteStateMachine<MinionFSMData>(minionFSMData);


        fsm.AddState(new CollectBallState(), true);
         fsm.AddState(new ThrowBallState());


        //MinionThrowTester, GameAIStudentWork, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
        //Debug.Log(this.GetType().AssemblyQualifiedName);

    }

    protected void Update()
    {
        fsm.Update();

        // For debugging, could repurpose the DisplayText of the Minion.
        // To do so affecting all states, implement the FSM's Update like so:
        //Minion.DisplayText(Minion.NavMeshCurrentSurfaceToString());

    }

}
