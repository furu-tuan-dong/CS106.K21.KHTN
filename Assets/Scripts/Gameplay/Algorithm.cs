﻿using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
//using System.Diagnostics;
using System.Linq;
//using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEditor;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

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
    //AI_Controller control = new AI_Controller();
    int numMummies;
    int select_algorithm;

    public GameState() { }

    public GameState(GameState other)
    {
        player = other.player;
        playerPos = other.playerPos;
        mummies = other.mummies;
        foreach (Vector3 mumPos in other.mummiesPos)
        {
            mummiesPos.Add(mumPos);
        }
        size = other.size;
        verticalWall = other.verticalWall;
        horizontalWall = other.horizontalWall;
        stairPosition = other.stairPosition;
        numMummies = other.numMummies;
    }

    public GameState(Character player, List<Character> mummies, int size,
        int[,] verticalWall, int[,] horizontalWall, Vector3 stairPosition, int algorithm)
    {
        this.player = player;
        playerPos = player.transform.localPosition;
        this.mummies = mummies;
        numMummies = mummies.Count;
        this.size = size;
        this.verticalWall = verticalWall;
        this.horizontalWall = horizontalWall;
        this.stairPosition = stairPosition;
        this.select_algorithm = algorithm;

        foreach (Character mummy in mummies)
        {
            mummiesPos.Add(mummy.transform.localPosition);
        }
    }

    public void Update(List<Character> mummies)
    {
        mummiesPos.Clear();
        // Update list mummies if the mummies conflict.
        foreach (Character mummy in mummies)
        {
            mummiesPos.Add(mummy.transform.localPosition);
        }
        numMummies = mummiesPos.Count;
    }

    public List<Vector3[]> Action()
    {
        List<Vector3[]> result = new List<Vector3[]>();

        //Player
        Vector3 playerAction = PlayerMove();
        if (!isBlocked(playerAction, playerPos))
        {
            Vector3[] playerResult = new Vector3[] { playerPos, playerAction };
            result.Add(playerResult);
            playerPos += playerAction;

            //Update mummies's position.
            MummiesMove(result);
        }

        return result;
    }
    Vector3 PlayerMove()
    {
        Vector3 action_ = Vector3.zero;
        if(select_algorithm == 1)
        {
            Minimax action = new Minimax(this, 3);
            action_ = action.GetAction();
        }
        else if (select_algorithm == 2)
        {
            Expectimax action = new Expectimax(this, 3);
            action_ = action.GetAction();
        }
        else if (select_algorithm == 3)
        {
            AlphaBeta action = new AlphaBeta(this, 4);
            action_ = action.GetAction();
        }
        return action_;
    }

    void MummiesMove(List<Vector3[]> result)
    {
        for (int i = 0; i < numMummies; i++)
        {
            Vector3 nextMove;
            Vector3[] resultMummy;
            if (mummies[i].tag == "WhiteMummy")
            {
                nextMove = WhiteTrace(mummiesPos[i])[0];
                resultMummy = new Vector3[] { mummiesPos[i], nextMove };
                result.Add(resultMummy);
            }
            else
            {
                nextMove = RedTrace(mummiesPos[i])[0];
                resultMummy = new Vector3[] { mummiesPos[i], nextMove };
                result.Add(resultMummy);
            }

            mummiesPos[i] += nextMove;
        }
    }

    List<Vector3> WhiteTrace(Vector3 position)
    {
        AstarSearch actions = new AstarSearch(this, position);
        List<Vector3> getAction = actions.GetActions();
        return getAction;
    }

    List<Vector3> RedTrace(Vector3 position)
    {
        AstarSearch actions = new AstarSearch(this, position);
        List<Vector3> getAction = actions.GetActions();
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

    public Vector3 GetStairPosition()
    {
        return stairPosition;
    }

    public List<Vector3> GetMummiesPosition()
    {
        return mummiesPos;
    }

    public void UpdatePlayer(Vector3 action)
    {
        playerPos += action;
    }

    public void UpdateMummy(Vector3 action, int index)
    {
        mummiesPos[index] += action;
    }

    public void Copy(GameState _state)
    {
        playerPos = _state.GetPlayerPosition();
        mummiesPos = _state.GetMummiesPosition();

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
        foreach (Vector3 path in preTracePath)
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

    public List<Vector3> GetActions()
    {
        Node start = new Node(startedPosition);
        List<Node> frontiers = new List<Node>();
        List<Vector3> closed = new List<Vector3>();
        frontiers.Add(start);

        if (isCatch(state, start.getPosition()))
        {
            List<Vector3> zero = new List<Vector3>() { Vector3.zero };
            return zero;
        }

        while (frontiers.Count != 0)
        {
            Node currentNode = frontiers[0];
            frontiers.RemoveAt(0);
            Vector3 currentPos = currentNode.getPosition();
            List<Vector3> currentTracePath = currentNode.getTracePath();

            if (isCatch(state, currentPos))
            {
                //return currentNode.getFirstDirection();
                return currentNode.getTracePath();
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


        return start.getTracePath();
    }

    void update(List<Node> list, Node insertedNode)
    {
        foreach (Node n in list)
        {
            if (n.getPosition() == insertedNode.getPosition())
            {
                if (n.getValue() <= insertedNode.getValue())
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

        if (list.Count == 0)
        {
            list.Add(insertedNode);
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (insertedNode.getValue() <= list[i].getValue())
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


class Minimax
{
    GameState state;
    int depth;

    public Minimax(GameState _state, int _depth)
    {
        state = new GameState(_state);
        depth = _depth;
    }

    public Vector3 GetAction()
    {
        return (Vector3)MinimaxAgent(state, 0, 0)[1];
        //return Vector3.left;
    }

    public ArrayList MinimaxAgent(GameState state, int _depth, int agentIndex)
    {
        Debug.Log(Mahattan(state.GetPlayerPosition(), state.GetStairPosition()));
        ArrayList result = new ArrayList();
        if (_depth == depth || IsLost(state) || IsWin(state))
        {

            result.Add(EvaluationFunction(state));
            result.Add(Vector3.zero);
            return result;
        }

        int nextAgentIndex = 0;
        if (agentIndex == state.GetMummiesPosition().Count)
        {
            nextAgentIndex = 0;
            _depth += 1;
        }
        else
        {
            nextAgentIndex += agentIndex + 1;
        }

        Vector3[] directions = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        foreach (Vector3 action in directions)
        {
            GameState nextState = new GameState(state);

            if (agentIndex == 0)
            {
                if (!state.isBlocked(action, state.GetPlayerPosition()))
                {
                    nextState.UpdatePlayer(action);
                }
                else
                {
                    continue;
                }
            }
            else
            {
                if (!state.isBlocked(action, state.GetMummiesPosition()[agentIndex - 1]))
                {
                    nextState.UpdateMummy(action, agentIndex - 1);
                }
                else
                {
                    continue;
                }
            }
            ArrayList value = MinimaxAgent(nextState, _depth, nextAgentIndex);

            if (result.Count == 0)
            {
                result.Add(value[0]);
                result.Add(action);
            }
            else
            {
                if (agentIndex == 0)
                {
                    if ((int)result[0] < (int)value[0])
                    {
                        result[0] = value[0];
                        result[1] = action;
                    }
                }
                else
                {
                    if ((int)result[0] > (int)value[0])
                    {
                        result[0] = value[0];
                        result[1] = action;
                    }
                }
            }

        }

        return result;
    }

    int EvaluationFunction(GameState s)
    {
        int eval = 0;

        eval += (-100) * Mahattan(s.GetPlayerPosition(), s.GetStairPosition());


        foreach (Vector3 mumPos in s.GetMummiesPosition())
        {
            if ((s.GetPlayerPosition() == mumPos) || (Mahattan(s.GetPlayerPosition(), mumPos) == 0) || (Mahattan(s.GetPlayerPosition(), mumPos) == 1))
            {
                eval += -10000;
            }
            else
            {
                eval += (100) * Mahattan(s.GetPlayerPosition(), mumPos);
            }

        }

        return eval;
    }


    int Mahattan(Vector3 position1, Vector3 position2)
    {
        int dis = (int)(Mathf.Abs(position1.x - position2.x) + Mathf.Abs(position1.y - position2.y) + Mathf.Abs(position1.z - position2.z));
        return dis;
    }


    bool IsLost(GameState state)
    {
        foreach (Vector3 mumPos in state.GetMummiesPosition())
        {
            if (mumPos == state.GetPlayerPosition())
            {
                return true;
            }
        }
        return false;
    }

    bool IsWin(GameState state)
    {
        if (state.GetStairPosition() == state.GetPlayerPosition())
        {
            return true;
        }
        return false;
    }
}

class AlphaBeta
{
    GameState state;
    int depth;

    public AlphaBeta(GameState _state, int _depth)
    {
        state = new GameState(_state);
        depth = _depth;
    }

    public Vector3 GetAction()
    {
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        return (Vector3)AlphaBetaAgent(state, 0, 0, alpha, beta)[1];
        //return Vector3.left;
    }

    public ArrayList AlphaBetaAgent(GameState state, int _depth, int agentIndex, int alpha, int beta)
    {
        ArrayList result = new ArrayList();
        if (_depth == depth || IsLost(state) || IsWin(state))
        {

            result.Add(EvaluationFunction(state));
            result.Add(Vector3.zero);
            return result;
        }

        int nextAgentIndex = 0;
        if (agentIndex == state.GetMummiesPosition().Count)
        {
            nextAgentIndex = 0;
            _depth += 1;
        }
        else
        {
            nextAgentIndex += agentIndex + 1;
        }

        Vector3[] directions = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        foreach (Vector3 action in directions)
        {
            GameState nextState = new GameState(state);

            if (agentIndex == 0)
            {
                if (!state.isBlocked(action, state.GetPlayerPosition()))
                {
                    nextState.UpdatePlayer(action);
                }
                else
                {
                    continue;
                }
            }
            else
            {
                if (!state.isBlocked(action, state.GetMummiesPosition()[agentIndex - 1]))
                {
                    nextState.UpdateMummy(action, agentIndex - 1);
                }
                else
                {
                    continue;
                }
            }

            if (result.Count == 0)
            {
                ArrayList value = AlphaBetaAgent(nextState, _depth, nextAgentIndex, alpha, beta);
                result.Add(value[0]);
                result.Add(action);
                if (agentIndex == 0)
                {
                    alpha = Math.Max(alpha, (int)result[0]);
                }
                else
                {
                    beta = Math.Min(beta, (int)result[0]);
                }
            }
            else
            {
                if ((int)result[0] > beta && agentIndex == 0)
                {
                    break;
                }

                if ((int)result[0] < alpha && agentIndex != 0)
                {
                    break;
                }

                ArrayList value = AlphaBetaAgent(nextState, _depth, nextAgentIndex, alpha, beta);

                if (agentIndex == 0)
                {
                    if ((int)value[0] > (int)result[0])
                    {
                        result[0] = value[0];
                        result[1] = action;
                        alpha = Math.Max(alpha, (int)result[0]);
                    }

                }
                else
                {
                    if ((int)result[0] > (int)value[0])
                    {
                        result[0] = value[0];
                        result[1] = action;
                        beta = Math.Min(beta, (int)result[0]);
                    }
                }
            }

        }

        return result;
    }

    int EvaluationFunction(GameState s)
    {
        int eval = 0;

        eval += (-10) * Mahattan(s.GetPlayerPosition(), s.GetStairPosition());


        foreach (Vector3 mumPos in s.GetMummiesPosition())
        {
            if (Mahattan(s.GetPlayerPosition(), mumPos) == 0)
            {
                eval += -10000;
            }
            else if (Mahattan(s.GetPlayerPosition(), mumPos) == 1)
            {
                eval += -5000;
            }
            else if (Mahattan(s.GetPlayerPosition(), mumPos) == 2)
            {
                eval += -1000;
            }
            else
            {
                eval += (100) * Mahattan(s.GetPlayerPosition(), mumPos);
            }

        }

        return eval;
    }


    int Mahattan(Vector3 position1, Vector3 position2)
    {
        int dis = (int)(Mathf.Abs(position1.x - position2.x) + Mathf.Abs(position1.y - position2.y) + Mathf.Abs(position1.z - position2.z));
        return dis;
    }


    bool IsLost(GameState state)
    {
        foreach (Vector3 mumPos in state.GetMummiesPosition())
        {
            if (mumPos == state.GetPlayerPosition())
            {
                return true;
            }
        }
        return false;
    }

    bool IsWin(GameState state)
    {
        if (state.GetStairPosition() == state.GetPlayerPosition())
        {
            return true;
        }
        return false;
    }
}

class Expectimax
{
    GameState state;
    int depth;

    public Expectimax(GameState _state, int _depth)
    {
        state = new GameState(_state);
        depth = _depth;
    }

    public Vector3 GetAction()
    {
        return (Vector3)ExpectimaxAgent(state, 0, 0)[1];
        //return Vector3.left;
    }


    public ArrayList ExpectimaxAgent(GameState state, int _depth, int agentIndex)
    {
        //Debug.Log(Mahattan(state.GetPlayerPosition(), state.GetStairPosition()));
        ArrayList result = new ArrayList();
        if (_depth == depth || IsLost(state) || IsWin(state))
        {
            result.Add(EvaluationFunction(state));
            result.Add(Vector3.zero);
            return result;
        }

        int nextAgentIndex = 0;
        if (agentIndex == state.GetMummiesPosition().Count) // number of mummies
        {
            nextAgentIndex = 0;
            _depth += 1;
        }
        else
        {
            nextAgentIndex += agentIndex + 1;
        }
        var v_player = double.NegativeInfinity;
        var direction = new Vector3();
        var v_mummy = 0.0;
        var numberofSuccessor = 0;
        Vector3[] directions = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };


        foreach (Vector3 action in directions)
        {
            GameState nextState = new GameState(state);

            if (agentIndex == 0)
            {
                if (!state.isBlocked(action, state.GetPlayerPosition()))
                {
                    nextState.UpdatePlayer(action);
                }
                else
                {
                    continue;
                }
            }
            else
            {
                if (!state.isBlocked(action, state.GetMummiesPosition()[agentIndex - 1]))
                {
                    nextState.UpdateMummy(action, agentIndex - 1);
                }
                else
                {
                    continue;
                }
            }


            ArrayList value = ExpectimaxAgent(nextState, _depth, nextAgentIndex);

            if (agentIndex == 0)
            {
                if (v_player < (double)value[0])
                {
                    v_player = (double)value[0];
                    direction = action;
                }
            }
            else
            {
                numberofSuccessor += 1;
                v_mummy = v_mummy + (double)value[0];
            }
        }
        if (agentIndex != 0)
        {
            v_mummy = v_mummy / numberofSuccessor;
            result.Add(v_mummy);
            result.Add(Vector3.zero); // this Add is useless. Because we never know exactly where the mummy gonna go
        }
        else
        {
            result.Add(v_player);
            result.Add(direction);
        }
        return result;
    }

    double EvaluationFunction(GameState s)
    {
        double eval = 0;

        eval += (-100) * Mahattan(s.GetPlayerPosition(), s.GetStairPosition());


        foreach (Vector3 mumPos in s.GetMummiesPosition())
        {
            if ((s.GetPlayerPosition() == mumPos) || (Mahattan(s.GetPlayerPosition(), mumPos) == 0) || (Mahattan(s.GetPlayerPosition(), mumPos) == 1))
            {
                eval += -10000;
            }
            else
            {
                eval += (500) * Mahattan(s.GetPlayerPosition(), mumPos);
            }

        }

        return eval;
    }


    int Mahattan(Vector3 position1, Vector3 position2)
    {
        int dis = (int)(Mathf.Abs(position1.x - position2.x) + Mathf.Abs(position1.y - position2.y) + Mathf.Abs(position1.z - position2.z));
        return dis;
    }


    bool IsLost(GameState state)
    {
        foreach (Vector3 mumPos in state.GetMummiesPosition())
        {
            if (mumPos == state.GetPlayerPosition())
            {
                return true;
            }
        }
        return false;
    }

    bool IsWin(GameState state)
    {
        if (state.GetStairPosition() == state.GetPlayerPosition())
        {
            return true;
        }
        return false;
    }
}
