using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;


public class MDPLevelController : MonoBehaviour
{
    // Inspector
    public GameObject winOverlay;
    public GameObject loseOverlay;
    public GameObject dust_effect;

    // Internal
    public double probability = 0.4;
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

    void Awake()
    {
        size = 6;
        idle = true;
        probability = Math.Min(1.0, probability);
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


    Array Value_iteration(int max_iter, double gamma = 0.9)
    {

        double[] v_values = new double[observation_space];
        Array.Clear(v_values, 0, v_values.Length); // set the v_values array to 0

        for (int i = 0; i < max_iter; i++)
        {
            double[] pre_v_values = new double[observation_space];
            Array.Copy(v_values, 0, pre_v_values, 0, observation_space);
            for (int state = 0; state < observation_space; state++)
            {
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
                if (state == 5 || state == 12 || state == 35 || state == 0)
                {
                    q_value = 1.0 * (0.0 + gamma * pre_v_values[state]);
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

                            double reward = 0.0;
                            if (next_state == 5 || next_state == 12 || next_state == 35)
                                reward = -1.0;
                            else
                            {
                                if (next_state == 0) reward = 1.0;
                            }

                            if (possible_action == 0)
                                q_value += 0.4 * (reward + gamma * pre_v_values[next_state]);

                            else
                                q_value += 0.3 * (reward + gamma * pre_v_values[next_state]);

                        }
                        q_values[action] = q_value;

                    }
                }
                //int best_action = Array.IndexOf(q_values, q_values.Max());
                v_values[state] = q_values.Max();

            }
            if (Enumerable.SequenceEqual(v_values, pre_v_values))
                break;

        }

        return v_values;
    }


    Array Policy_extraction(double[] v_values, double gamma = 0.9)
    {
        Vector3[] policy = new Vector3[observation_space];
        Array.Clear(policy, 0, policy.Length); // set the v_values array to 0
        for (int state = 0; state < observation_space; state++)
        {
            double[] q_values = new double[action_space];
            double q_value = 0.0;
            /// get current position
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
            ///
            if (state == 5 || state == 12 || state == 35 || state == 0)
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

                        double reward = 0.0;
                        if (next_state == 5 || next_state == 12 || next_state == 35)
                            reward = -1.0;
                        else
                        {
                            if (next_state == 0) reward = 1.0;
                        }

                        if (possible_action == 0)
                            q_value += 0.4 * (reward + gamma * v_values[next_state]);

                        else
                            q_value += 0.3 * (reward + gamma * v_values[next_state]);

                    }
                    q_values[action] = q_value;

                }
            }
            int best_action = Array.IndexOf(q_values, q_values.Max());
            switch (best_action)
            {
                case 0:
                    policy[state] = Vector3.up;
                    break;
                case 1:
                    policy[state] = Vector3.right;
                    break;
                case 2:
                    policy[state] = Vector3.down;
                    break;
                case 3:
                    policy[state] = Vector3.left;
                    break;
            }
        }

        return policy;
    }

    // Update is called once per frame
    void Update()
    {
        if (!idle) return;
        Vector3 direction = Vector3.zero;

        //if (Input.GetKeyDown("up")) direction = Vector3.up;
        //else if (Input.GetKeyDown("down")) direction = Vector3.down;
        //else if (Input.GetKeyDown("left")) direction = Vector3.left;
        //else if (Input.GetKeyDown("right")) direction = Vector3.right;


        double[] value_iter = new double[observation_space];
        Array.Copy(Value_iteration(1000), 0, value_iter, 0, observation_space);

        Vector3[] policy_iter = new Vector3[observation_space];
        Array.Copy(Policy_extraction(value_iter), 0, policy_iter, 0, observation_space);


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

        direction = policy_iter[player_position];

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

        if (probRandom <= 0.8)
        {
            return possible_actions[0];
        }
        else
        {
            if (probRandom > 0.8 || probRandom <= 0.9)
            {
                return possible_actions[1];
            }
            else
            {
                return possible_actions[2];
            }
        }

        //double minProb = Math.Min(probForInput, probForOther);
        //bool isInputMinProb = minProb == probForInput;
        //if ((probRandom <= minProb && isInputMinProb) || (probRandom > minProb && !isInputMinProb)){
        //    return inputDirection;
        //}
        //do{
        //    returnDirection = allDirection[randomObj.Next(0, 3)];
        //} while(returnDirection == inputDirection);
        //return returnDirection;
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
