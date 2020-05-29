using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
//using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GameState
{
    Character player;
    Vector3 playerPos;
    List<Character> mummies;
    public List<Vector3> mummiesPos = new List<Vector3>();
    int size;
    int[,] verticalWall;
    int[,] horizontalWall;
    Vector3 stairPosition;
    //private bool get_pos = true;
    AI_Controller control = new AI_Controller();
    int numMummies;

    public GameState() { }
    public GameState(Character player, List<Character> mummies, int size,
        int[,] verticalWall, int[,] horizontalWall, Vector3 stairPosition)
    {
        this.player = player;
        playerPos = player.transform.localPosition;
        this.mummies = mummies;
        numMummies = mummies.Count;
        this.size = size;
        this.verticalWall = verticalWall;
        this.horizontalWall = horizontalWall;
        this.stairPosition = stairPosition;

        foreach(Character mummy in mummies)
        {
            mummiesPos.Add(mummy.transform.localPosition);
        }
    }

    public List<Vector3[]> Action(Vector3 direction)
    {
        List<Vector3[]> result = new List<Vector3[]>();
        if (!isBlocked(direction, playerPos))
        {
            //Player
            Vector3[] playerResult = new Vector3[] { playerPos, direction };
            result.Add(playerResult);
            playerPos += direction;

            //Update mummies's position.
            MummiesMove(result);
        }

        return result;
    }

    void MummiesMove(List<Vector3[]> result)
    {
        for(int i = 0; i < numMummies; i++)
        {
            Vector3 nextMove;
            Vector3[] resultMummy;
            if(mummies[i].tag == "WhiteMummy")
            {
                nextMove = WhiteTrace(mummiesPos[i]);
                resultMummy = new Vector3[] { mummiesPos[i], nextMove };
                result.Add(resultMummy);
            }
            else
            {
                nextMove = RedTrace(mummiesPos[i]);
                resultMummy = new Vector3[] { mummiesPos[i], nextMove };
                result.Add(resultMummy);
            }

            mummiesPos[i] += nextMove;
        }
    }
   
    Vector3 WhiteTrace(Vector3 position)
    {
        AstarSearch actions = new AstarSearch(this, position);
        Vector3 getAction = actions.GetActions();
        return getAction;
    }

    Vector3 RedTrace(Vector3 position)
    {
        AstarSearch actions = new AstarSearch(this, position);
        Vector3 getAction = actions.GetActions();
        return getAction;
    }
    
    public bool isBlocked(Vector3 direction, Vector3 position)
    {
        int x = (int)position.x;
        int y = (int)position.y;
        int n = size - 1;

        if (direction == Vector3.up)
            return y == n || horizontalWall[x, y + 1] == 1;

        if (direction == Vector3.down)
            return y == 0 || horizontalWall[x, y] == 1;

        if (direction == Vector3.left)
            return x == 0 || verticalWall[x, y] == 1;

        if (direction == Vector3.right)
            return x == n || verticalWall[x + 1, y] == 1;

        return true;
    }

    public Vector3 GetPlayerPosition()
    {
        return playerPos;
    }

  
}

public class Node
{
    Vector3 position;
    List<Vector3> tracePath;
    int value;
    
    public Node(Vector3 position)
    {
        this.position = position;
        tracePath = new List<Vector3>();
        value = 0;
    }

    public Vector3 getPosition()
    {
        return position;
    }

    public Vector3 getFirstDirection()
    {
        return tracePath[0];
    }

    public void updateValue(int heuristicValue)
    {
        value = heuristicValue + tracePath.Count;
    }

    public void setTracePath(List<Vector3> preTracePath, Vector3 direction)
    {
        foreach(Vector3 path in preTracePath)
        {
            tracePath.Add(path);
        }
        tracePath.Add(direction);
    }

    public List<Vector3> getTracePath()
    {
        return tracePath;
    }

    public int getValue()
    {
        return value;
    }
}

public class AstarSearch
{
    GameState state = new GameState();
    Vector3 startedPosition;
    public AstarSearch(GameState state, Vector3 startedPosition)
    {
        this.state = state; 
        this.startedPosition = startedPosition;
    }
    
    public Vector3 GetActions()
    {
        Node start = new Node(startedPosition);
        List<Node> frontiers = new List<Node>();
        List<Vector3> closed = new List<Vector3>();
        frontiers.Add(start);

        if (isCatch(state, start.getPosition()))
        {
            return Vector3.zero;
        }


        while (frontiers.Count != 0)
        {
            Node currentNode = frontiers[0];
            frontiers.RemoveAt(0);
            Vector3 currentPos = currentNode.getPosition();
            List<Vector3> currentTracePath = currentNode.getTracePath();

            if (isCatch(state, currentPos))
            {
                return currentNode.getFirstDirection();
            }

            closed.Add(currentPos);

            Vector3[] directions = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
            foreach (Vector3 direction in directions)
            {

                if (!state.isBlocked(direction, currentPos))
                {
                    Node newNode = new Node(currentPos + direction);
                    newNode.setTracePath(currentTracePath, direction);
                    newNode.updateValue(Heuristic(state, newNode.getPosition()));

                    if (!closed.Contains(newNode.getPosition()))
                    {
                        update(frontiers, newNode);
                    }
                }
            }
        }

        return Vector3.zero;
    }

    void update(List<Node> list, Node insertedNode)
    {
        foreach(Node n in list)
        {
            if(n.getPosition() == insertedNode.getPosition())
            {
                if(n.getValue() <= insertedNode.getValue())
                {
                    return;
                }
                else
                {
                    list.Remove(n);
                    break;
                }
            }
        }

        if(list.Count == 0)
        {
            list.Add(insertedNode);
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                if(insertedNode.getValue() <= list[i].getValue())
                {
                    list.Insert(i, insertedNode);
                    return;
                }
            }
            list.Add(insertedNode);
        }
    }

    bool isCatch(GameState state, Vector3 position)
    {
        if (position == state.GetPlayerPosition())
        {
            return true;
        }
        return false;
    }

    int Heuristic(GameState state, Vector3 position)
    {
        int disManhatan = (int)(Mathf.Abs(position.x - state.GetPlayerPosition().x) + Mathf.Abs(position.y - state.GetPlayerPosition().y));
        return disManhatan;
    }
}
