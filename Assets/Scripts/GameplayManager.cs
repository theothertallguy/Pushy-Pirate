using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Tilemaps;
using System.Runtime.CompilerServices;
using System;

public enum Hits
{
    Wall,
    Nothing
}

public struct SwapPair
{
    public SwapPair(Vector3Int close, Vector3Int far)
    {
        CloseBlock = close;
        FarBlock = far;
    }

    public Vector3Int CloseBlock { get; }
    public Vector3Int FarBlock { get; }

    // Override ToString to provide a meaningful string representation
    public override string ToString()
    {
        return $"CloseBlock: {CloseBlock}, FarBlock: {FarBlock}";
    }
}

public struct ActionFrame
{
    public ActionFrame(Vector3Int pDir, List<string> dest, List<Vector3Int> mov, List<SwapPair> swaps)
    {
        PlayerDir = pDir;
        Destroyed = dest;
        Moved = mov;
        SwapPairs = swaps;
    }

    public Vector3Int PlayerDir { get; }
    public List<string> Destroyed { get; }
    public List<Vector3Int> Moved { get; }
    public List<SwapPair> SwapPairs { get; }

    // Override ToString to provide a meaningful string representation
    public override string ToString()
    {
        // Simply join the list elements without checking for null, since they're guaranteed to be non-null
        string destroyedStr = string.Join(", ", Destroyed);
        string movedStr = string.Join(", ", Moved);
        string swapPairsStr = string.Join(", ", SwapPairs.Select(s => s.ToString()));

        // Return formatted string
        return $"PlayerDir: {PlayerDir}, Destroyed: [{destroyedStr}], Moved: [{movedStr}], SwapPairs: [{swapPairsStr}]";
    }
}

public class GameplayManager : MonoBehaviour
{
    public PlayerController player;
    public List<MatchBlocks> blocks;
    public Tilemap walls;
    public List<Vector3Int> toMove;
    public List<Vector3Int> moved;
    public List<Vector3Int> matched;
    public List<string> unmatched;
    public int level;
    public List<ActionFrame> ActionQueue;
    public float speed = 5f;

    public int blocksAreMoving = 0;

    public Dictionary<Vector3Int, MatchBlocks> blockMap;
    public Dictionary<string, Vector3Int> matchMap;

    // Start is called before the first frame update
    void Start()
    {
        level = int.Parse(SceneManager.GetActiveScene().name.Substring(6));
        blockMap = new Dictionary<Vector3Int, MatchBlocks>();
        matchMap = new Dictionary<string, Vector3Int>();
        foreach (MatchBlocks b in blocks)
        {
            blockMap.Add(Integerize(b.transform.position), b);
        }
        ActionQueue = new List<ActionFrame>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("r"))
        {
            Restart();
        }
    }

    // fully loads the scene again
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // turn vector3s into vector3int to avoid floating point errors
    public Vector3Int Integerize(Vector3 vector)
    {
        return new Vector3Int((int)Math.Round(vector.x), (int)Math.Round(vector.y), (int)Math.Round(vector.z));
    }

    // level number
    public int GetLevel()
    {
        return level;
    }

    // is the named block in the blocks dictionary?
    public bool hasBlock(string blockName)
    {
        foreach (var item in blocks)
        {
            if (blockName == item.name)
            {
                return true;
            }
        }
        return false;
    }

    // check if a source standing at pos moving in direction dir would cause a collision into a wall
    public Hits CollisionCheck(Vector3Int pos, Vector3Int dir)
    {
        Vector3Int candidatePosition = pos + dir;
        if (!walls.HasTile(Vector3Int.RoundToInt(pos += dir)))
        {
            if (blockMap.ContainsKey(pos))
            {
                Hits status = CollisionCheck(candidatePosition, dir);
                if (status == Hits.Nothing)
                {
                    toMove.Add(candidatePosition);
                }
                return status;
            }
            else
            {
                return Hits.Nothing;
            }
        }
        else
        {
            return Hits.Wall;
        }
    }

    // move the blocks the player has pushed in direction dir
    public List<Vector3Int> MoveBlocks(Vector3Int dir)
    {
        foreach (Vector3Int block in new List<Vector3Int>(toMove))
        {
            moved.Add(blockMap[block].PushBlock(dir));
        }

        toMove.Clear();
        return moved;
    }

    // check if there are 3 or more connected blocks of the same type
    public List<string> CheckForMatches()
    {
        List<string> matches = new List<string>();
        foreach (var block in new List<Vector3Int>(moved))
        {
            if (blockMap.ContainsKey(block))
            {
                matches.AddRange(MatchThree(block));
            }
        }
        moved.Clear();
        return matches;
    }

    // remove old block from blocks dictionary
    public void UpdateBlock(Vector3Int old, MatchBlocks neue)
    {
        blockMap.Remove(old);
        blockMap.Add(Integerize(neue.GetXYZ()), neue);
    }

    // count contiguous blocks of same type
    int matchCount = 0;
    public List<string> MatchThree(Vector3Int leader)
    {
        List<string> matches = new List<string>();
        GetSameNeighbors(leader, blockMap[leader].GetSpriteName());
        if (matchCount >= 3)
        {
            foreach (var block in matched)
            {
                matches.Add(blockMap[block].name);
            }
            ChangeVisibilityOfMatchBlocks();
        }
        matched.Clear();
        matchCount = 0;
        return matches;
    }

    // recursively check neighbors to see if they have been checked for equivalence yet
    void GetSameNeighbors(Vector3Int home, string str)
    {
        if (blockMap.ContainsKey(home) && str == blockMap[home].GetSpriteName() && !matched.Contains(home))
        {
            matched.Add(home);
            matchCount++;
            GetSameNeighbors(home + Vector3Int.left, str);
            GetSameNeighbors(home + Vector3Int.right, str);
            GetSameNeighbors(home + Vector3Int.up, str);
            GetSameNeighbors(home + Vector3Int.down, str);
        }
    }

    // tries to swap blocks in all 4 directions
    public List<SwapPair> TrySwap(Vector3Int cent)
    {
        List<SwapPair> pairs = new List<SwapPair>();
        pairs.AddRange(SwapHelper(cent, Vector3Int.left));
        pairs.AddRange(SwapHelper(cent, Vector3Int.up));
        pairs.AddRange(SwapHelper(cent, Vector3Int.right));
        pairs.AddRange(SwapHelper(cent, Vector3Int.down));
        return pairs;
    }

    // swaps two specified blocks if they exist
    public List<SwapPair> SwapHelper(Vector3Int cent, Vector3Int dir)
    {
        List<SwapPair> pairs = new List<SwapPair>();
        Vector3Int close = cent + dir;
        Vector3Int far = close + dir;

        if (blockMap.ContainsKey(far) && blockMap.ContainsKey(close))
        {
            pairs.Add(Swap(close, far));
            moved.Add(close);
            moved.Add(far);
        }
        return pairs;
    }

    // swap position of blocks fluidly
    public SwapPair Swap(Vector3Int close, Vector3Int far)
    {
        MatchBlocks temp = blockMap[far];
        blockMap[far] = blockMap[close];
        blockMap[close] = temp;
        blockMap[close].SetXYZ(close);
        blockMap[far].SetXYZ(far);
        return new SwapPair(far, close);
    }

    // hide and 'destroy' matched blocks by making them invisible and uninteractible
    public void ChangeVisibilityOfMatchBlocks()
    {
        foreach (Vector3Int b in matched)
        {
            if (blockMap.ContainsKey(b))
            {
                matchMap.Add(blockMap[b].name, b);
                blockMap[b].ChangeVisibility();
                blockMap.Remove(b);
            }
        }
        if (blockMap.Count == 0)
        {
            SceneManager.LoadScene("Level " + (level + 1));
        }

        foreach (string s in unmatched)
        {
            if (matchMap.ContainsKey(s))
            {
                blockMap.Add(matchMap[s], GameObject.Find(s).GetComponent<MatchBlocks>());
                blockMap[matchMap[s]].ChangeVisibility();
                matchMap.Remove(s);
            }
        }
    }

    // add an action frame for the undo tracker
    public void AddActionFrame(ActionFrame af)
    {
        ActionQueue.Add(af);
    }

    // undo movement by reversing all previous movement in last action frame
    public void Undo()
    {
        if (ActionQueue.Any())
        {
            //Debug.Log("verdammnt");
            /*
            ActionFrame has 4 components
            PlayerDir: the direction the player moved in that Frame
            Destroyed: All MatchBlocks matched and destroyed during that frame, the Vector3 is the key to be removed brok matchMap and added back to blockMap
            Moved: All MatchBlocks that were moved in PlayerDir, the Vector3 is their current key in blockMap
            SwapPairs: List of the (0-4) pairs of MatckBlocks swapped
            */
            ActionFrame step = ActionQueue.Last();
            Vector3Int undoDir = step.PlayerDir * -1;
            //Debug.Log(undoDir);
            // Undo PlayerDir
            player.MoveActually(undoDir);
            // Undo Destroyed
            unmatched = step.Destroyed;
            ChangeVisibilityOfMatchBlocks();
            unmatched.Clear();
            //Undo Moved
            toMove = step.Moved;
            toMove.Reverse();
            MoveBlocks(undoDir);
            toMove.Clear();
            moved.Clear();
            // Undo SwapPairs
            foreach (SwapPair pair in step.SwapPairs)
            {
                Swap(pair.CloseBlock, pair.FarBlock);
            }
            // Remove ActionFrame from ActionQueue
            ActionQueue.RemoveAt(ActionQueue.Count - 1);
        }
    }

    // gets the speed the blocks and players move at in comparison to delta time
    public float getSpeed() {
        return speed;
    }
}