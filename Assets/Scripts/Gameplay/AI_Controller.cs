using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;

public class AI_Controller : MonoBehaviour
{
    class Checkpoint {
        public Vector3 player;
        public List<Vector3> mummies = new List<Vector3>();
    }

    // Select algorithm
    public int Select_Algorithm;

    public int Depth;

    // Inspector
    public GameObject winOverlay;
    public GameObject loseOverlay;
    public GameObject dust_effect;
    public GameObject defeat_effect;
    
    // Internal
    public bool idle;
    public Character player;
    public List<Character> mummies;
    public Stack checkpoints = new Stack();
    
    // Static
    public int size;
    int[,] verticalWall;
    int[,] horizontalWall;
    Vector3 stairPosition;
    Vector3 stairDirection;
    bool restrictedVision = false;


    List<Vector3[]> actions = new List<Vector3[]>();

    //Counting time
    Stopwatch time;


    void Awake() {
        size = 6;
        idle = true;
        mummies = new List<Character>();
        verticalWall = new int[size, size];
        horizontalWall = new int[size, size];
    }

    GameState controlState;


    // Start is called before the first frame update
    void Start()
    {
        int n = size;
        foreach (Transform t in transform) {
            int x = (int) t.localPosition.x;
            int y = (int) t.localPosition.y;

            switch (t.tag) {
                case "Player":
                    player = t.GetComponent<Character>();
                    break;
                case "WhiteMummy":
                case "RedMummy":
                    mummies.Add(t.GetComponent<Character>());
                    break;
                case "Stair":
                    stairPosition = t.localPosition;
                    if (x == 0) stairDirection = Vector3.left;
                    if (y == 0) stairDirection = Vector3.down;
                    if (x == n) {
                        stairPosition.x--;
                        stairDirection = Vector3.right;
                    }
                    if (y == n) {
                        stairPosition.y--;
                        stairDirection = Vector3.up;
                    }
                    break;
                case "VerticalWall":
                    verticalWall[x, y] = 1;
                    break;
                case "HorizontalWall":
                    horizontalWall[x, y] = 1;
                    break;
                default:
                    UnityEngine.Debug.Log("Unexpected game object with tag: " + t.tag);
                    break;
            }
        }

        controlState = new GameState(player, mummies, size, verticalWall, horizontalWall, stairPosition, Select_Algorithm, Depth);
    }

    
    // Update is called once per frame
    void Update()
    {
        if (!idle)
        {
            return;
        }

        StartCoroutine(Action());
    }

    public IEnumerator Action() 
    {
        time = new Stopwatch();
        time.Start();

        actions = controlState.Action();
        if (actions.Count == 0) yield break;
  
        Vector3 playerAction = actions[0][1];

        // Player move 1 step
        if (Blocked(player.transform.localPosition, playerAction)) yield break;
        

        idle = false;
        yield return player.Move(playerAction, false);

        if (MummiesCatch())
        {
            yield return Lost();
            yield break;
        }

        yield return MummiesMove(actions);

        if (MummiesCatch())
        {
            yield return Lost();
            yield break;
        }

        yield return MummiesFight();


        if (player.transform.localPosition == stairPosition)
        {
            yield return Victory();
            yield break;
        }

        idle = true;
    }

    // Win and lose
    IEnumerator Victory() 
    {
        time.Stop();
        UnityEngine.Debug.Log($"{time.ElapsedMilliseconds} ms");

        yield return player.Move(stairDirection, false);

        Destroy(player.gameObject);
        foreach (var mummy in mummies) 
            Destroy(mummy.gameObject);

        yield return new WaitForSeconds(0.5f);
        Instantiate(winOverlay, transform, true);

    }

    IEnumerator Lost()
    {
        time.Stop();
        UnityEngine.Debug.Log($"{time.ElapsedMilliseconds} ms");

        Vector3 position = player.transform.localPosition;

        Destroy(player.gameObject);
        foreach (var mummy in mummies) 
            Destroy(mummy.gameObject);

        yield return RunEffect(defeat_effect, position, false);
        yield return RunEffect(dust_effect, position, true);

        yield return new WaitForSeconds(0.5f);
        Instantiate(loseOverlay, transform, true);    
    }

    IEnumerator MummiesMove(List<Vector3[]> actions) {
        List<IEnumerator> coroutines = new List<IEnumerator>();

        foreach (var mummy in mummies)
        {
            Vector3 nextMove = Vector3.zero;
            foreach(Vector3[] posAct in actions)
            {
                if(mummy.transform.localPosition == posAct[0])
                {
                    nextMove = posAct[1];
                }
            }
            //Vector3 next_move = mummy.tag == "WhiteMummy"
            //    ? state.getWhiteTrace(mummy.transform.localPosition)
            //    : state.getRedTrace(mummy.transform.localPosition);
            
            bool isBlocked = Blocked(mummy.transform.localPosition, nextMove);
            
            coroutines.Add(mummy.Move(nextMove, isBlocked));
        }

        yield return PromiseAll(coroutines.ToArray());
    }

    bool MummiesCatch() {
        foreach (var mummy in mummies) {
            if (mummy.transform.localPosition == player.transform.localPosition)
                return true;
        }
        return false;
    }

    IEnumerator MummiesFight()
    {
        // Group mummy by position
        var positions = new Dictionary<Vector3, List<Character>>();

        foreach (var mummy in mummies)
        {
            Vector3 key = mummy.transform.localPosition;
            if (!positions.ContainsKey(key))
                positions.Add(key, new List<Character>());

            positions[key].Add(mummy);
        }

        //Delete mummy and run effect
        var effects = new List<IEnumerator>();

        foreach (var item in positions)
        {
            if (item.Value.Count == 1) continue;
            
            // Preserve one
            item.Value.RemoveAt(0);
            foreach (var mummy in item.Value)
            {
                mummies.Remove(mummy);
                Destroy(mummy.gameObject);
            }

            effects.Add(RunEffect(dust_effect, item.Key, true));
        }

        yield return PromiseAll(effects.ToArray());
        controlState.Update(mummies);
    }

    // Character vs walls
    public bool Blocked(Vector3 position, Vector3 direction) {
        int x = (int)position.x;
        int y = (int)position.y;
        int n = size-1;
        
        if (direction == Vector3.up)
            return y == n || horizontalWall[x, y+1] == 1;

        if (direction == Vector3.down)
            return y == 0 || horizontalWall[x, y] == 1;

        if (direction == Vector3.left)
            return x == 0 || verticalWall[x, y] == 1;
        
        if (direction == Vector3.right)
            return x == n || verticalWall[x+1, y] == 1;
        
        return true;
    }


    // Helper functions
    IEnumerator RunEffect(GameObject effect, Vector3 position, bool clear) {
        GameObject fx = Instantiate(effect, transform);
        fx.transform.localPosition = position;
        yield return fx.GetComponent<Effect>().Run(clear);
    }
    
    IEnumerator PromiseAll(params IEnumerator[] coroutines) {
        while (true) {
            object current = null;

            foreach (IEnumerator x in coroutines) {
                if (x.MoveNext() == true)
                    current = x.Current;
            }

            if (current == null) break;
            yield return current;
        }
    }

    // Undo
    public void Save() {
        var checkpoint = new Checkpoint();
        checkpoint.player = player.transform.localPosition;
        foreach (var mummy in mummies) {
            checkpoint.mummies.Add(mummy.transform.localPosition);
        }
        checkpoints.Push(checkpoint);
    }

    public void Restore() {
        var checkpoint = (Checkpoint)checkpoints.Pop();
        
    }
}
