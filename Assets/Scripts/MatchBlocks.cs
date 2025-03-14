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

        if (gm != null && notAdded && !gm.hasBlock(this.name)) {
            gm.blocks.Add(this);
        }
    }

    void OnDestroy() {
        //do this when more spoons
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position != movePos) {
            if (!iAmMoving) {
                gm.blocksAreMoving++;
                iAmMoving = true;
            }
            transform.position = Vector3.MoveTowards(transform.position, movePos, gm.getSpeed() * Time.deltaTime);
            if (transform.position == movePos && iAmMoving) {
                iAmMoving = false;
                gm.blocksAreMoving--;
            }
        }
    }

    public Vector3 GetXYZ()
    {
        return movePos;
    }

    public void SetXYZ(Vector3 pos)
    {
        movePos = pos;
    }

    public string GetSpriteName()
    {
        return this.GetComponent<SpriteRenderer>().sprite.name;
    }

    public Vector3 PushBlock(Vector3 dir)
    {
        movePos += dir;
        gm.UpdateBlock(transform.position, this);
        return GetXYZ();
    }

    public void ChangeVisibility()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        bool state = spriteRenderer.enabled;
        spriteRenderer.enabled = !state;
    }
}
