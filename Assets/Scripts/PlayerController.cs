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

    // Start is called before the first frame update
    void Start()
    {
        movePos = transform.position;
    }

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

    void OnDestroy() {
        gm.player = null;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, movePos, gm.getSpeed() * Time.deltaTime);
        if (transform.position == movePos && 0 == gm.blocksAreMoving)
        {
            if (Input.GetKeyDown("w") || Input.GetKey("w"))
            {
                MoveAttempt(Vector3.up);
            }
            else if (Input.GetKeyDown("a") || Input.GetKey("a"))
            {
                MoveAttempt(Vector3.left);
            }
            else if (Input.GetKeyDown("s") || Input.GetKey("s"))
            {
                MoveAttempt(Vector3.down);
            }
            else if (Input.GetKeyDown("d") || Input.GetKey("d"))
            {
                MoveAttempt(Vector3.right);
            }
            else if (Input.GetKeyDown("space"))
            {
                Swap();
            }
            else if (Input.GetKeyDown("q") || Input.GetKey("q"))
            {
                gm.Undo();
            }
        }
    }

    void MoveAttempt(Vector3 dir)
    {
        Vector3 temp = transform.position;
        Hits status = gm.CollisionCheck(temp, dir);
        if (status == Hits.Nothing)
        {
            movePos += dir;
            List<Vector3> moved = new(gm.MoveBlocks(dir));
            List<string> destroyed = new(gm.CheckForMatches());
            gm.AddActionFrame(new ActionFrame(dir, destroyed, moved, new List<SwapPair>()));
            Debug.Log(new ActionFrame(dir, destroyed, moved, new List<SwapPair>()));
        }
    }

    void Swap()
    {
        if (gm.GetLevel() >= 4)
        {
            List<SwapPair> pairs = new(gm.TrySwap(transform.position));
            List<string> destroyed = new(gm.CheckForMatches());
            gm.AddActionFrame(new ActionFrame(Vector3.zero, destroyed, new List<Vector3>(), pairs));
        }
    }

    public void MoveActually(Vector3 dir)
    {
        movePos += dir;
    }
}
