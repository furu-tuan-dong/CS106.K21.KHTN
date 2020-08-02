﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MDPPolicy : MonoBehaviour
{
    // Inspector
    public GameObject winOverlay;
    public GameObject loseOverlay;
    public GameObject dust_effect;

    // Internal
    public double probability;
    public double gamma;
    public double Step_reward;
    public double Stair_reward;
    public double Hole_reward;
    public bool idle;
    public Character player;


    // Static
    public int size;
    int[,] hole;
    int[,] verticalWall;
    int[,] horizontalWall;
    Vector3 stairPosition;
    Vector3 stairDirection;
    bool restrictedVision = false;


    // value iteration
    int observation_space = 36;
    int action_space = 4;
    bool isFirst = true;
    int STAIR_POS = 0;
    ArrayList HOLE_LIST = new ArrayList();
    int[] policy_iter = new int[36];
    void Awake()
    {
        size = 6;
        idle = true;
        probability = Math.Min(1.0, probability);
        gamma = Math.Min(1.0, gamma);
        Step_reward = Math.Min(100.0, Step_reward);
        Stair_reward = Math.Min(100.0, Stair_reward);
        Hole_reward = Math.Min(100.0, Hole_reward);
        //mummies = new List<Character>();
        verticalWall = new int[size, size];
        horizontalWall = new int[size, size];
        hole = new int[size, size];
    }
    // Start is called before the first frame update
    void Start()
    {
        int n = size;
        foreach (Transform t in transform)
        {
            int x = (int)t.localPosition.x;
            int y = (int)t.localPosition.y;

            switch (t.tag)
            {
                case "Player":
                    player = t.GetComponent<Character>();
                    break;
                case "Stair":
                    stairPosition = t.localPosition;
                    if (x == 0) stairDirection = Vector3.left;
                    if (y == 0) stairDirection = Vector3.down;
                    if (x == n)
                    {
                        stairPosition.x--;
                        stairDirection = Vector3.right;
                    }
                    if (y == n)
                    {
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
                case "Hole":
                    hole[x, y] = 1;

                    break;
                default:
                    Debug.Log("Unexpected game object with tag: " + t.tag);
                    break;
            }
        }
    }
    Array Compute_value_policy(int[] policy, double gamma, ArrayList hole_list, int stair_pos)
    {
        double[] v_values = new double[observation_space];
        Array.Clear(v_values, 0, v_values.Length); // set the v_values array to 0

        while (true)
        {
            double[] pre_v_values = new double[observation_space];
            Array.Copy(v_values, 0, pre_v_values, 0, observation_space);



            for (int state = 0; state < observation_space; state++) //check 36 states
            {
                double p_action = policy[state];
                double q_value = 0.0;

                Vector3 cur_pos = new Vector3
                {
                    x = state % 6
                };
                if (state >= 30 && state <= 35) cur_pos.y = 0;
                else if (state >= 24 && state <= 29) cur_pos.y = 1;
                else if (state >= 18 && state <= 23) cur_pos.y = 2;
                else if (state >= 12 && state <= 17) cur_pos.y = 3;
                else if (state >= 6 && state <= 11) cur_pos.y = 4;
                else if (state >= 0 && state <= 5) cur_pos.y = 5;
                
                if (hole_list.Contains(state) || state == stair_pos){
                    q_value = 0.0;
                }
                else{
                    int[] possible_actions = new int[3];
                    if (p_action == 0) { possible_actions[0] = 0; possible_actions[1] = 3; possible_actions[2] = 1; }
                    if (p_action == 1) { possible_actions[0] = 1; possible_actions[1] = 0; possible_actions[2] = 2; }
                    if (p_action == 2) { possible_actions[0] = 2; possible_actions[1] = 1; possible_actions[2] = 3; }
                    if (p_action == 3) { possible_actions[0] = 3; possible_actions[1] = 2; possible_actions[2] = 0; }

                    for (int possible_action = 0; possible_action < possible_actions.Length; possible_action++)
                    {
                        int next_state = 0;
                        switch (possible_actions[possible_action])
                        {
                            case 0:
                                if (Blocked(cur_pos, Vector3.up) || (state - 6 < 0))
                                    next_state = state;
                                else
                                    next_state = state - 6;
                                break;
                            case 1:
                                if (Blocked(cur_pos, Vector3.right) || (((state + 1) % 6) == 0))
                                    next_state = state;
                                else
                                    next_state = state + 1;
                                break;
                            case 2:
                                if (Blocked(cur_pos, Vector3.down) || (state + 6 > 35))
                                    next_state = state;
                                else
                                    next_state = state + 6;
                                break;
                            case 3:
                                if (Blocked(cur_pos, Vector3.left) || ((state % 6) == 0))
                                    next_state = state;
                                else
                                    next_state = state - 1;
                                break;

                        }

                        double reward = Step_reward;
                        if (hole_list.Contains(next_state)) reward = Hole_reward;
                        else if (next_state == stair_pos) reward = Stair_reward;

                        if (possible_action == 0)
                            q_value += probability * (reward + gamma * pre_v_values[next_state]);

                        else
                            q_value += ((1.0 - probability) / 2) * (reward + gamma * pre_v_values[next_state]);

                    }
                }
                
                v_values[state] = q_value;
            }
            if (Enumerable.SequenceEqual(v_values, pre_v_values))
                break;
        }
        return v_values;
    }


    Array Extract_policy(double []v_values, double gamma, ArrayList hole_list, int stair_pos)
    {
        int[] policy = new int[observation_space];
        Array.Clear(policy, 0, policy.Length); // set the v_values array to 0


        for (int state=0; state< observation_space; state++)
        {
            double[] act_values = new double[action_space];
            Array.Clear(act_values, 0, act_values.Length); // set the v_values array to 0

            //get current position 
            Vector3 cur_pos = new Vector3
            {
                x = state % 6
            };
            if (state >= 30 && state <= 35) cur_pos.y = 0;
            else if (state >= 24 && state <= 29) cur_pos.y = 1;
            else if (state >= 18 && state <= 23) cur_pos.y = 2;
            else if (state >= 12 && state <= 17) cur_pos.y = 3;
            else if (state >= 6 && state <= 11) cur_pos.y = 4;
            else if (state >= 0 && state <= 5) cur_pos.y = 5;

            if (hole_list.Contains(state) || state == stair_pos){
                for (int i = 0; i < 4; i++){
                    act_values[i] = 0;
                }
            }
            else{
                for (int action = 0; action < action_space; action++)
                {
                    int[] possible_actions = new int[3]; // each direction got 3 possible actions

                    /// 0 : UP
                    /// 1 : RIGHT
                    /// 2 : DOWN
                    /// 3 : LEFT
                    if (action == 0) { possible_actions[0] = 0; possible_actions[1] = 3; possible_actions[2] = 1; } //UP : 3 possible actions are LEFT, UP, RIGHT
                    if (action == 1) { possible_actions[0] = 1; possible_actions[1] = 0; possible_actions[2] = 2; } //RIGHT : 3 possible actions are RIGHT, UP, DOWN
                    if (action == 2) { possible_actions[0] = 2; possible_actions[1] = 1; possible_actions[2] = 3; } //DOWN : 3 possible actions are LEFT, DOWN, RIGHT
                    if (action == 3) { possible_actions[0] = 3; possible_actions[1] = 2; possible_actions[2] = 0; } //LEFT : 3 possible actions are LEFT, UP, DOWN

                    for (int possible_action = 0; possible_action < possible_actions.Length; possible_action++) // check 3 possible action and set the next state
                    {
                        int next_state = 0;
                        switch (possible_actions[possible_action])
                        {
                            case 0:
                                if (Blocked(cur_pos, Vector3.up) || (state - 6 < 0))
                                    next_state = state;
                                else
                                    next_state = state - 6;
                                break;
                            case 1:
                                if (Blocked(cur_pos, Vector3.right) || (((state + 1) % 6) == 0))
                                    next_state = state;
                                else
                                    next_state = state + 1;
                                break;
                            case 2:
                                if (Blocked(cur_pos, Vector3.down) || (state + 6 > 35))
                                    next_state = state;
                                else
                                    next_state = state + 6;
                                break;
                            case 3:
                                if (Blocked(cur_pos, Vector3.left) || ((state % 6) == 0))
                                    next_state = state;
                                else
                                    next_state = state - 1;
                                break;

                        }
                        double reward = Step_reward;
                        if (hole_list.Contains(next_state)) reward = Hole_reward;
                        else if (next_state == stair_pos) reward = Stair_reward;


                        if (possible_action == 0)
                            act_values[action] += probability * (reward + gamma * v_values[next_state]);

                        else
                            act_values[action] += ((1.0 - probability) / 2) * (reward + gamma * v_values[next_state]);
                    }
                }
            }
            policy[state] = Array.IndexOf(act_values, act_values.Max());
        }
        return policy;
    }
    Array Policy_iteration(int max_iters, double gamma, ArrayList hole_list, int stair_pos)
    {
        System.Random rnd = new System.Random();
        int[] policy = new int[observation_space];
        for (int i = 0; i < observation_space; i++) policy[i] = rnd.Next(4);

        for (int i = 0; i < max_iters; i++)
        {
            double[] pre_policy_v = new double[observation_space];
            Array.Copy(Compute_value_policy(policy, gamma, hole_list, stair_pos), 0, pre_policy_v, 0, observation_space);
            int[] new_policy = new int[observation_space];
            Array.Copy(Extract_policy(pre_policy_v, gamma, hole_list, stair_pos), 0, new_policy, 0, observation_space);

            if (Enumerable.SequenceEqual(policy, new_policy))
                break;

            Array.Copy(new_policy, 0, policy, 0, observation_space);
        }
        
        return policy;
    }

    void Update()
    {
        if (!idle) return;
        
        if (isFirst){
            for (int x = 0; x < 6; x++)
            {
                int pos = 0;
                for (int y = 0; y < 6; y++)
                {
                    if (y == 0) pos = 30 + x;
                    else if (y == 1) pos = 24 + x;
                    else if (y == 2) pos = 18 + x;
                    else if (y == 3) pos = 12 + x;
                    else if (y == 4) pos = 6 + x;
                    else if (y == 5) pos = x;

                    if (hole[x, y] == 1) HOLE_LIST.Add(pos);
                    else if (x == stairPosition.x && y == stairPosition.y) STAIR_POS = pos;
                }
            }
            Array.Copy(Policy_iteration(1000, gamma, HOLE_LIST, STAIR_POS), 0, policy_iter, 0, observation_space);
            isFirst = false;
        }
        Vector3 direction = Vector3.zero;


        // get player position
        int player_position = 0;
        int x_pos = (int)player.transform.localPosition.x;
        switch (player.transform.localPosition.y)
        {
            case 0:
                player_position = 30 + x_pos;
                break;
            case 1:
                player_position = 24 + x_pos;
                break;
            case 2:
                player_position = 18 + x_pos;
                break;
            case 3:
                player_position = 12 + x_pos;
                break;
            case 4:
                player_position = 6 + x_pos;
                break;
            case 5:
                player_position = 0 + x_pos;
                break;
        }
        //convert number to direction
        switch (policy_iter[player_position])
        {
            case 0:
                direction = Vector3.up;
                break;
            case 1:
                direction = Vector3.right;
                break;
            case 2:
                direction = Vector3.down;
                break;
            case 3:
                direction = Vector3.left;
                break;
        }
        direction = getDirectionForAgent(direction, probability);

        if (direction != Vector3.zero)
            StartCoroutine(Action(direction));
    }

    Vector3 getDirectionForAgent(Vector3 inputDirection, double probForInput)
    {
        if (inputDirection == Vector3.zero) return inputDirection;
        //Vector3 returnDirection;
        List<Vector3> allDirection = new List<Vector3>() { Vector3.up, Vector3.down, Vector3.right, Vector3.left };
        double probForOther = (1 - probForInput) / 2;
        System.Random randomObj = new System.Random();
        double probRandom = randomObj.NextDouble();

        Vector3[] possible_actions = new Vector3[3];
        if (inputDirection == Vector3.up) { possible_actions[0] = Vector3.up; possible_actions[1] = Vector3.left; possible_actions[2] = Vector3.right; }
        if (inputDirection == Vector3.right) { possible_actions[0] = Vector3.right; possible_actions[1] = Vector3.up; possible_actions[2] = Vector3.down; }
        if (inputDirection == Vector3.down) { possible_actions[0] = Vector3.down; possible_actions[1] = Vector3.right; possible_actions[2] = Vector3.left; }
        if (inputDirection == Vector3.left) { possible_actions[0] = Vector3.left; possible_actions[1] = Vector3.down; possible_actions[2] = Vector3.up; }


        if (probRandom <= probability)
        {
            return possible_actions[0];
        }
        else
        {
            if ((probRandom > probability) && (probRandom <= (probability + (1.0 - probability) / 2)))
            {
                return possible_actions[1];
            }
            else
            {
                return possible_actions[2];
            }
        }

    }
    IEnumerator Action(Vector3 direction)
    {

        // Player move 1 step
        if (Blocked(player.transform.localPosition, direction)) yield break;

        idle = false;
        yield return player.Move(direction, false);
        if (FallHoles())
        {
            yield return Lost();
            yield break;
        }
        if (player.transform.localPosition == stairPosition)
        {
            yield return Victory();
            yield break;
        }

        idle = true;
    }
    IEnumerator Victory()
    {
        yield return player.Move(stairDirection, false);

        Destroy(player.gameObject);

        yield return new WaitForSeconds(0.5f);
        Instantiate(winOverlay, transform, true);
    }
    IEnumerator Lost()
    {
        Vector3 position = player.transform.localPosition;

        Destroy(player.gameObject);

        //yield return RunEffect(defeat_effect, position, false);
        yield return RunEffect(dust_effect, position, true);

        yield return new WaitForSeconds(0.5f);
        Instantiate(loseOverlay, transform, true);
    }
    bool FallHoles()
    {
        Vector3 playerPosistion = player.transform.localPosition;
        int x = (int)playerPosistion.x;
        int y = (int)playerPosistion.y;
        return hole[x, y] == 1 ? true : false;
    }
    bool Blocked(Vector3 position, Vector3 direction)
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
    IEnumerator RunEffect(GameObject effect, Vector3 position, bool clear)
    {
        GameObject fx = Instantiate(effect, transform);
        fx.transform.localPosition = position;
        yield return fx.GetComponent<Effect>().Run(clear);
    }
}
