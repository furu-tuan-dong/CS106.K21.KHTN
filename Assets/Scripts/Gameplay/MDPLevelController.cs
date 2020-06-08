using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
public class MDPLevelController : MonoBehaviour
{
    // Inspector
    public GameObject winOverlay;
    public GameObject loseOverlay;
    public GameObject dust_effect;

    // Internal
    public double probability = 0.8;
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

    void Awake() {
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
        foreach (Transform t in transform) {
            int x = (int) t.localPosition.x;
            int y = (int) t.localPosition.y;

            switch (t.tag) {
                case "Player":
                    player = t.GetComponent<Character>();
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
                case "Hole":
                    hole[x, y] = 1;
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

        direction = getDirectionForAgent(direction, probability);

        if (direction != Vector3.zero)
            StartCoroutine(Action(direction));
    }
    Vector3 getDirectionForAgent(Vector3 inputDirection, double probForInput){
        if (inputDirection == Vector3.zero) return inputDirection;
        Vector3 returnDirection;
        List<Vector3> allDirection = new List<Vector3>(){Vector3.up, Vector3.down, Vector3.right, Vector3.left};
        double probForOther = (1 -probForInput) / 3;
        System.Random randomObj = new System.Random();
        double probRandom = randomObj.NextDouble(); 
        double minProb = Math.Min(probForInput, probForOther);
        bool isInputMinProb = minProb == probForInput;
        if ((probRandom <= minProb && isInputMinProb) || (probRandom > minProb && !isInputMinProb)){
            return inputDirection;
        }
        do{
            returnDirection = allDirection[randomObj.Next(0, 3)];
        } while(returnDirection == inputDirection);
        return returnDirection;
    }
    IEnumerator Action(Vector3 direction) {

        // Player move 1 step
        if (Blocked(player.transform.localPosition, direction)) yield break;

        idle = false;
        yield return player.Move(direction, false);
        if (FallHoles()){
            yield return Lost();
            yield break;
        }
        if (player.transform.localPosition == stairPosition) {
            yield return Victory();
            yield break;
        }
        
        idle = true;
    }
    IEnumerator Victory() {
        yield return player.Move(stairDirection, false);

        Destroy(player.gameObject);

        yield return new WaitForSeconds(0.5f);
        Instantiate(winOverlay, transform, true);
    }
    IEnumerator Lost() {
        Vector3 position = player.transform.localPosition;

        Destroy(player.gameObject);

        //yield return RunEffect(defeat_effect, position, false);
        yield return RunEffect(dust_effect, position, true);

        yield return new WaitForSeconds(0.5f);
        Instantiate(loseOverlay, transform, true);
    }
    bool FallHoles() {
        Vector3 playerPosistion = player.transform.localPosition;
        int x = (int) playerPosistion.x;
        int y = (int) playerPosistion.y;
        return hole[x,y] == 1 ? true : false;
    }
    bool Blocked(Vector3 position, Vector3 direction) {
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
    IEnumerator RunEffect(GameObject effect, Vector3 position, bool clear) {
        GameObject fx = Instantiate(effect, transform);
        fx.transform.localPosition = position;
        yield return fx.GetComponent<Effect>().Run(clear);
    }
}
