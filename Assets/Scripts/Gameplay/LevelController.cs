using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class LevelController : MonoBehaviour
{
    class Checkpoint {
        public Vector3 player;
        public List<Vector3> mummies = new List<Vector3>();
    }


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


    void Awake() {
        size = 6;
        idle = true;
        mummies = new List<Character>();
        verticalWall = new int[size, size];
        horizontalWall = new int[size, size];
    }

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
                    Debug.Log("Unexpected game object with tag: " + t.tag);
                    break;
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!idle) return;
        Vector3 direction = Vector3.zero;

        if (Input.GetKeyDown("up")) direction = Vector3.up;
        else if (Input.GetKeyDown("down")) direction = Vector3.down;
        else if (Input.GetKeyDown("left")) direction = Vector3.left;
        else if (Input.GetKeyDown("right")) direction = Vector3.right;
        
        /*int i = 0;
        while(true)
        {
            if (!idle) return;
            Vector3 direction = Vector3.zero;
            if (i % 2 == 0) direction = Vector3.up;
            else direction = Vector3.down;
            if (direction != Vector3.zero)
                StartCoroutine(Action(direction));
            i++;
        }*/


            if (direction != Vector3.zero)
                StartCoroutine(Action(direction));
    }

    IEnumerator Action(Vector3 direction) {

        // Player move 1 steps
        if (Blocked(player.transform.localPosition, direction)) yield break;

        idle = false;
        yield return player.Move(direction, false);

       
        // Mummy move 2 steps
        for (int step = 0; step < 2; step++) {
            
            yield return MummiesMove();

            if (MummiesCatch()) {
                yield return Lost();
                yield break;
            }

            yield return MummiesFight();
        }

        if (player.transform.localPosition == stairPosition) {
            yield return Victory();
            yield break;
        }
        
        idle = true;
    }

    // Win and lose
    IEnumerator Victory() {
        yield return player.Move(stairDirection, false);

        Destroy(player.gameObject);
        foreach (var mummy in mummies) 
            Destroy(mummy.gameObject);

        yield return new WaitForSeconds(0.5f);
        Instantiate(winOverlay, transform, true);
    }

    IEnumerator Lost() {
        Vector3 position = player.transform.localPosition;

        Destroy(player.gameObject);
        foreach (var mummy in mummies) 
            Destroy(mummy.gameObject);

        yield return RunEffect(defeat_effect, position, false);
        yield return RunEffect(dust_effect, position, true);

        yield return new WaitForSeconds(0.5f);
        Instantiate(loseOverlay, transform, true);
    }

    List<((int , int), Vector3 , int)> GetSuccessors(Vector3 position)
    {
        
        int px = (int)position.x;
        int py = (int)position.y;
        
        var successors = new List<((int, int), Vector3, int)>();
        
        
        for (int i  = 0; i < 4; i++) // loop through 4 directions
        {
            var action = new Vector3();
            var nextx = 0;
            var nexty = 0;
            //var (nextx, nexty) = (0, 0);
            if (i == 0)
            {
                (nextx, nexty) = (px - 1, py); // WEST
                action = Vector3.left;
            }
            if (i == 1)
            {
                (nextx, nexty) = (px + 1, py); // EAST
                action = Vector3.right;
            }
            if (i == 2)
            {
                (nextx, nexty) = (px, py - 1); // SOUTH
                action = Vector3.down;
            }
            if (i == 3)
            {
                (nextx, nexty) = (px, py + 1); // NORTH
                action = Vector3.up;
            }
           
            //Blocked(position, action);
            if (!Blocked(position, action))
            { 
                var nextState = (nextx, nexty);
                var cost = 1;
                var tmp = (nextState, action, cost); // dont know how to add directly to successors              
                successors.Add(tmp);
                
            }
        }
        return successors;
    }
    double EuclideanDistance(int x, int y, int px, int py)
    {
        
        return Math.Sqrt(Math.Pow(x - px, 2) + Math.Pow(y - py, 2));
    }
      
    ArrayList AstarSearch(Vector3 position)
    {
        
        int x = (int)player.transform.localPosition.x;
        int y = (int)player.transform.localPosition.y;
        int z = (int)player.transform.localPosition.z;
        int px = (int)position.x;
        int py = (int)position.y;
        int pz = (int)position.z;
        var startingNode = (px, py); // starting point (x,y)
        var goalNode = (x, y); // player position 
        
        var visitedNodes = new ArrayList(); //visited nodes


        var path = new ArrayList // initial the path array 
        {
            Vector3.zero 
        }; // a list of vector3 (Vector3.up, Vector3.down, Vector3.left, Vector3.right)
        if (px == x && py == y) return path; // the mummies caught the player

        
        List<(((int, int), ArrayList, double), double)> pQueuelist = new List<(((int, int), ArrayList, double), double)> // List of nodes will be processed
        {
            ((startingNode, path, 0.0), 0.0) // the fisrt item in the pQueuelist
        };

        while (pQueuelist.Count != 0)
        {

            var (currentNode, actions, prevCost) = (pQueuelist[0].Item1.Item1, pQueuelist[0].Item1.Item2, pQueuelist[0].Item1.Item3);
            pQueuelist.RemoveAt(0); // remove the first item after adding its successor to the list

            if (!visitedNodes.Contains(currentNode)) // check whether the current is in the list of visited nodes
            {
                visitedNodes.Add(currentNode);


                if (currentNode.Equals(goalNode)) //check whether the mummy reaches the goal 
                    return actions;
                


                Vector3 tmp = new Vector3
                {
                    x = currentNode.Item1,
                    y = currentNode.Item2
                };
                var successors = GetSuccessors(tmp); // return number of possible moves

                for (int i = 0; i < successors.Count; i++)
                {
                    var (nextNode, action, cost) = (successors[i].Item1, successors[i].Item2, successors[i].Item3);


                    var newActions = new ArrayList(); // new set of action
                    newActions = (ArrayList)actions.Clone();
                    newActions.Add(action);

                    double newCostToNode = prevCost + cost;
                    double heuristicCost = newCostToNode + EuclideanDistance(nextNode.Item1, nextNode.Item2, goalNode.x, goalNode.y);
                    var tmp1 = ((nextNode, newActions, newCostToNode), heuristicCost); // create the tmp1 cause dont know how to add directly to the pQueuelist

                    if (pQueuelist.Count >= 1) // sorting and adding item to the pQueuelist (295 -> 313)
                    {
                        int check = 0;
                        for (int j = 0; j < pQueuelist.Count; j++)
                        {
                            if (heuristicCost < pQueuelist[j].Item2)
                            {
                                check = 1;
                                pQueuelist.Insert(j, tmp1);
                                break;
                            }

                        }
                        if (check == 0)
                        {
                            pQueuelist.Add(tmp1);
                        }
                    }
                    else pQueuelist.Add(tmp1);
                }       
            }

        }        
        return path;
    }

    // Mummies  
    Vector3 WhiteTrace(Vector3 position) {
        var directions = AstarSearch(position);
    


        /*int x = (int) player.transform.localPosition.x;
        int y = (int) player.transform.localPosition.y;
        int z = (int) player.transform.localPosition.z;
        int px = (int) position.x;
        int py = (int) position.y;
        int pz = (int) position.z;

        if (x > px) {
            if (!Blocked(position, Vector3.right)) return Vector3.right;
        }
        if (x < px) {
            if (!Blocked(position, Vector3.left)) return Vector3.left;
        }
        if (y > py) return Vector3.up;
        if (y < py) return Vector3.down;
        if (x > px) return Vector3.right;
        if (x < px) return Vector3.left;*/
        if (directions[1].Equals(Vector3.up)) return Vector3.up;
        if (directions[1].Equals(Vector3.down)) return Vector3.down;
        if (directions[1].Equals(Vector3.left)) return Vector3.left;
        if (directions[1].Equals(Vector3.right)) return Vector3.right;
        return Vector3.zero;
        
        
    }

    Vector3 RedTrace(Vector3 position) {
        int x = (int) player.transform.localPosition.x;
        int y = (int) player.transform.localPosition.y;
        int z = (int) player.transform.localPosition.z;
        int px = (int) position.x;
        int py = (int) position.y;
        int pz = (int) position.z;

        if (y > py) {
            if (!Blocked(position, Vector3.up)) return Vector3.up;
        }
        if (y < py) {
            if (!Blocked(position, Vector3.down)) return Vector3.down;
        }
        if (x > px) return Vector3.right;
        if (x < px) return Vector3.left;
        if (y > py) return Vector3.up;
        if (y < py) return Vector3.down;

        return Vector3.zero;
    }

    IEnumerator MummiesMove() {
        List<IEnumerator> coroutines = new List<IEnumerator>();
        
        foreach (var mummy in mummies) {
            Vector3 next_move = mummy.tag == "WhiteMummy"
                ? WhiteTrace(mummy.transform.localPosition)
                : RedTrace(mummy.transform.localPosition);
            
            bool isBlocked = Blocked(mummy.transform.localPosition, next_move);
            
            coroutines.Add(mummy.Move(next_move, isBlocked));
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

    IEnumerator MummiesFight() {
        // Group mummy by position
        var positions = new Dictionary<Vector3, List<Character>>();

        foreach (var mummy in mummies) {
            Vector3 key = mummy.transform.localPosition;
            if (!positions.ContainsKey(key))
                positions.Add(key, new List<Character>());
            
            positions[key].Add(mummy);
        }

        //Delete mummy and run effect
        var effects = new List<IEnumerator>();
        
        foreach (var item in positions) {
            if (item.Value.Count == 1) continue;

            // Preserve one
            item.Value.RemoveAt(0);
            foreach (var mummy in item.Value) {
                mummies.Remove(mummy);
                Destroy(mummy.gameObject);
            }
            
            effects.Add(RunEffect(dust_effect, item.Key, true));
        }

        yield return PromiseAll(effects.ToArray());
    }

    // Character vs walls
    bool Blocked(Vector3 position, Vector3 direction) {
        int x = (int)position.x;
        int y = (int)position.y;
        int n = size-1;

        if (direction == Vector3.up)
        {
            
            return y == n || horizontalWall[x, y + 1] == 1;
        }

        if (direction == Vector3.down)
        {
            
            return y == 0 || horizontalWall[x, y] == 1;
        }

        if (direction == Vector3.left)
        {
            
            return x == 0 || verticalWall[x, y] == 1;
        }

        if (direction == Vector3.right)
        {
           
            return x == n || verticalWall[x + 1, y] == 1;
        }
        
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
        var checkpoint = new Checkpoint
        {
            player = player.transform.localPosition
        };
        foreach (var mummy in mummies) {
            checkpoint.mummies.Add(mummy.transform.localPosition);
        }
        checkpoints.Push(checkpoint);
    }

    public void Restore() {
        var checkpoint = (Checkpoint)checkpoints.Pop();
        
    }
}

internal class List<T1, T2>
{
    public List()
    {
    }
}
