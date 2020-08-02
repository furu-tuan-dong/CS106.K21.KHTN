using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MultiController : MonoBehaviour
{
    // Internal
    public double probability;
    public double gamma;
    public double Step_reward;
    public double Stair_reward;
    public double Hole_reward;
    public bool idle;
    public Character player;

    public int NUM_RUN;
    public int typeMDP;
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
    int STAIR_POS = 0;
    ArrayList HOLE_LIST = new ArrayList();
    ArrayList VWALL_LIST = new ArrayList();
    ArrayList HWALL_LIST = new ArrayList();

    int[] policy_iter = new int[36];
    void Awake()
    {
        size = 6;
        idle = true;
        typeMDP = 0;
        probability = Math.Min(1.0, probability);
        gamma = Math.Min(1.0, gamma);
        Step_reward = Math.Min(100.0, Step_reward);
        Stair_reward = Math.Min(100.0, Stair_reward);
        Hole_reward = Math.Min(100.0, Hole_reward);
        NUM_RUN = Math.Min(2000, NUM_RUN);
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
                else if (horizontalWall[x, y] == 1) HWALL_LIST.Add(pos);
                else if (verticalWall[x, y] == 1) VWALL_LIST.Add(pos);
                else if (x == stairPosition.x && y == stairPosition.y) STAIR_POS = pos;
            }
        }
        //Policy Iteration
        if (typeMDP == 1){
            Array.Copy(Policy_iteration(1000, gamma, HOLE_LIST, STAIR_POS), 0, policy_iter, 0, observation_space);
        }
        //Value iteration
        else if (typeMDP == 0){
            double[] value_iter = new double[observation_space];
            Array.Copy(Value_iteration(10000, HOLE_LIST, STAIR_POS, gamma), 0, value_iter, 0, observation_space);
            Array.Copy(Policy_extraction(value_iter, HOLE_LIST, STAIR_POS, gamma), 0, policy_iter, 0, observation_space);
        }

        for (int i = 0; i < 36; i++)
        {
            Debug.Log(policy_iter[i]);
        }

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
        int win = 0;
        int lose = 0;
        for (int i = 0; i < NUM_RUN; i++){
            int currentState = player_position;
            while(true){
                int ac = policy_iter[currentState];
                int direc = getDirection(ac);
                if (currentState + direc == STAIR_POS){
                    win += 1;
                    break;
                }
                // Debug.Log(direc);
                //int draftState = currentState;
                if (direc == 0 && currentState / 6  > 0) currentState =  !HWALL_LIST.Contains(currentState - 6) ? currentState - 6 : currentState;
                else if(direc == 1 && currentState % 6 != 5) currentState = !VWALL_LIST.Contains(currentState + 1) ? currentState + 1 : currentState;
                else if (direc == 2 && currentState / 6 < 5) currentState =  !HWALL_LIST.Contains(currentState + 6) ? currentState + 6 : currentState;
                else if (direc == 3 && currentState % 6 != 0) currentState = !VWALL_LIST.Contains(currentState - 1) ? currentState - 1 : currentState;
                //Fall hole
                if (HOLE_LIST.Contains(currentState)){
                    lose += 1;
                    break;
                }
                //Victory
                
                //Debug.Log(currentState);
            }
        }
        Debug.Log(win);
        Debug.Log(lose);
    }
    //Policy iteration
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
    //Value iteration
    Array Value_iteration(int max_iter, ArrayList hole_list, int stair_pos, double gamma)
    {
        
        double[] v_values = new double[observation_space]; 
        Array.Clear(v_values, 0, v_values.Length); // set the v_values array to 0

        for (int i = 0; i < max_iter; i++)
        {
            double[] pre_v_values = new double[observation_space];
            Array.Copy(v_values, 0, pre_v_values, 0, observation_space);
            for (int state = 0; state < observation_space; state++) //check 36 states
            {

                //get current position of the explorer
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



                double[] q_values = new double[action_space];
                Array.Clear(q_values, 0, q_values.Length);
                double q_value = 0.0;

                if (hole_list.Contains(state) || state == stair_pos) // if state is in hole_list
                {
                    q_value = 1.0 * (0.0 + gamma * pre_v_values[state]); // so the game end
                    q_values[0] = q_value;
                    for (int tmp = 1; tmp < 4; tmp++) q_values[tmp] = 0;
                }
                else
                {
                    for (int action = 0; action < action_space; action++) // check 4 directions
                    {
                        q_value = 0.0;
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
                                q_value += probability * (reward + gamma * pre_v_values[next_state]);

                            else
                                q_value += ((1.0 - probability) / 2) * (reward + gamma * pre_v_values[next_state]);
                        }
                        q_values[action] = q_value;

                    }
                }
                v_values[state] = q_values.Max();

            }
            
            if (Enumerable.SequenceEqual(v_values, pre_v_values))
                break;
            
        }
        //for (int i = 0; i < 36; i++)
        //{
        //    Debug.Log(i + 1 + ": " + v_values[i]);
        //}
        return v_values;
    }


    Array Policy_extraction(double []v_values, ArrayList hole_list, int stair_pos,double gamma)
    {
        
        int[] policy = new int[observation_space];
        Array.Clear(policy, 0, policy.Length); // set the v_values array to 0
        for (int state = 0; state < observation_space; state++)
        {
            double[] q_values = new double[action_space];
            double q_value = 0.0;

            /// get current position of the explorer
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


            if (hole_list.Contains(state) || state == stair_pos)
            {
                q_value = 1.0 * (0.0 + gamma * v_values[state]);
                q_values[0] = q_value;
                for (int tmp = 1; tmp < 4; tmp++) q_values[tmp] = 0;
            }
            else
            {
                for (int action = 0; action < action_space; action++)
                {
                    q_value = 0.0;
                    int[] possible_actions = new int[3];

                    /// 0 : UP
                    /// 1 : RIGHT
                    /// 2 : DOWN
                    /// 3 : LEFT
                    if (action == 0) { possible_actions[0] = 0; possible_actions[1] = 3; possible_actions[2] = 1; }
                    if (action == 1) { possible_actions[0] = 1; possible_actions[1] = 0; possible_actions[2] = 2; }
                    if (action == 2) { possible_actions[0] = 2; possible_actions[1] = 1; possible_actions[2] = 3; }
                    if (action == 3) { possible_actions[0] = 3; possible_actions[1] = 2; possible_actions[2] = 0; }

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
                            q_value += probability * (reward + gamma * v_values[next_state]);

                        else
                            q_value += ((1.0 - probability) / 2) * (reward + gamma * v_values[next_state]);

                    }
                    q_values[action] = q_value;

                }
            }
            
            int best_action = Array.IndexOf(q_values, q_values.Max());
            policy[state] = best_action;
        }
        
        return policy;
    }

    void Update()
    {
        if (!idle) return;
    }
    int getDirection(int action){
        // 0: UP
        // 1: RIGHT
        // 2: DOWN
        // 3: LEFT
        int[] possible_actions = new int[3];
        if (action == 0) { possible_actions[0] = 0; possible_actions[1] = 3; possible_actions[2] = 1; }
        if (action == 1) { possible_actions[0] = 1; possible_actions[1] = 0; possible_actions[2] = 2; }
        if (action == 2) { possible_actions[0] = 2; possible_actions[1] = 1; possible_actions[2] = 3; }
        if (action == 3) { possible_actions[0] = 3; possible_actions[1] = 2; possible_actions[2] = 0; }

        System.Random randomObj = new System.Random();
        double probRandom = randomObj.NextDouble();
        if (probRandom <= probability)
        {
            return possible_actions[0];
        }
        else
        {
            if ((probRandom <= (probability + (1.0 - probability) / 2)))
            {
                return possible_actions[1];
            }
            else
            {
                return possible_actions[2];
            }
        }
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
}
