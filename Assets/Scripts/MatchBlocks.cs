using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Tilemaps;

public class MatchBlocks : MonoBehaviour
{
    public GameplayManager gm;
    public Vector3 movePos;

    private bool notAdded = true;

    public bool iAmMoving = false;

    // Start is called before the first frame update
    void Start()
    {
        movePos = transform.position;
    }

    // add blocks to gameplay manager when they are added to the scene
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

        if (gm != null && notAdded && !gm.hasBlock(this.name))
        {
            gm.blocks.Add(this);
            notAdded = false;
        }
    }

    // remove block from gameplay manager when removed from scene
    void OnDestroy() {
        //do this when more spoons
    }

    // Update is called once per frame
    void Update()
    {
        // I need to move to match my world coordinates
        if (transform.position != movePos)
        {
            if (!iAmMoving)
            {
                gm.blocksAreMoving++;
                iAmMoving = true;
            }
            transform.position = Vector3.MoveTowards(transform.position, movePos, gm.getSpeed() * Time.deltaTime);
            if (transform.position == movePos && iAmMoving)
            {
                iAmMoving = false;
                gm.blocksAreMoving--;
            }
        }
    }

    // set a new position in the coordinate system
    public Vector3 GetXYZ()
    {
        return movePos;
    }

    // set block position on the coordinate grid
    public void SetXYZ(Vector3 pos)
    {
        movePos = pos;
    }

    // get this block's sprite name
    public string GetSpriteName()
    {
        return this.GetComponent<SpriteRenderer>().sprite.name;
    }

    // move block in specified direction by 1 unit vector length
    public Vector3Int PushBlock(Vector3Int dir)
    {
        movePos += dir;
        gm.UpdateBlock(gm.Integerize(transform.position), this);
        return gm.Integerize(GetXYZ());
    }

    // hide the block and remove it from play after it was matched
    public void ChangeVisibility()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        bool state = spriteRenderer.enabled;
        spriteRenderer.enabled = !state;
    }
}
