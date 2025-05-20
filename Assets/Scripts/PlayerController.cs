using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public GameplayManager gm;

    public Vector3 movePos;
    private int action;

    // Start is called before the first frame update
    void Start()
    {
        // set player position to parent GameObject position
        movePos = transform.position;
    }

    // Adds the GameplayManager in the scdene to the gm field, and sets the player in gm to this
    void OnValidate()
    {
        if (gm == null)
        {
            GameObject gameController = GameObject.FindWithTag("GameController");
            if (gameController != null)
            {
                gm = gameController.GetComponent<GameplayManager>();
            }
        }

        if (gm != null) {
            gm.player = this;
        }
    }

    // remove this from gm when removed from the scene
    void OnDestroy() {
        gm.player = null;
    }

    // action variable to set functionality for Android buttons
    public void SetButtonAction(int act)
    {
        action = act;
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position == movePos && 0 == gm.blocksAreMoving)
        {
            if (Input.GetKeyDown("w") || Input.GetKey("w") || action == 1)
            {
                MoveAttempt(Vector3Int.up);
            }
            else if (Input.GetKeyDown("a") || Input.GetKey("a") || action == 2)
            {
                MoveAttempt(Vector3Int.left);
            }
            else if (Input.GetKeyDown("s") || Input.GetKey("s") || action == 3)
            {
                MoveAttempt(Vector3Int.down);
            }
            else if (Input.GetKeyDown("d") || Input.GetKey("d") || action == 4)
            {
                MoveAttempt(Vector3Int.right);
            }
            else if (Input.GetKeyDown("space") || action == 5)
            {
                Swap();
            }
            else if (Input.GetKeyDown("q") || Input.GetKey("q") || action == 6)
            {
                gm.Undo();
            }
            action = 0;
        }
        else
        {
            // player moves uniformly towards the new integer coordinates
            transform.position = Vector3.MoveTowards(transform.position, movePos, gm.getSpeed() * Time.deltaTime);
        }
    }

    // try to move the player, if there isn't anything blocking the path
    void MoveAttempt(Vector3Int dir)
    {
        Vector3Int temp = gm.Integerize(transform.position);
        Hits status = gm.CollisionCheck(temp, dir);
        if (status == Hits.Nothing)
        {
            movePos += dir;
            List<Vector3Int> moved = new(gm.MoveBlocks(dir));
            List<string> destroyed = new(gm.CheckForMatches());
            gm.AddActionFrame(new ActionFrame(dir, destroyed, moved, new List<SwapPair>()));
            //Debug.Log(new ActionFrame(dir, destroyed, moved, new List<SwapPair>()));
        }
    }

    // Try to swap the blocks orthogonal to the player
    void Swap()
    {
        if (gm.GetLevel() >= 4)
        {
            List<SwapPair> pairs = new(gm.TrySwap(gm.Integerize(transform.position)));
            List<string> destroyed = new(gm.CheckForMatches());
            gm.AddActionFrame(new ActionFrame(Vector3Int.zero, destroyed, new List<Vector3Int>(), pairs));
        }
    }

    //move the player's integer coordinate by 1 unit (a unit vector)
    public void MoveActually(Vector3 dir)
    {
        movePos += dir;
    }
}
