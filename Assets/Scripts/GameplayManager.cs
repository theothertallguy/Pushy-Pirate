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
    public SwapPair(Vector3 close, Vector3 far)
    {
        CloseBlock = close;
        FarBlock = far;
    }

    public Vector3 CloseBlock { get; }
    public Vector3 FarBlock { get; }

    // Override ToString to provide a meaningful string representation
    public override string ToString()
    {
        return $"CloseBlock: {CloseBlock}, FarBlock: {FarBlock}";
    }
}

public struct ActionFrame
{
    public ActionFrame(Vector3 pDir, List<string> dest, List<Vector3> mov, List<SwapPair> swaps)
    {
        PlayerDir = pDir;
        Destroyed = dest;
        Moved = mov;
        SwapPairs = swaps;
    }

    public Vector3 PlayerDir { get; }
    public List<string> Destroyed { get; }
    public List<Vector3> Moved { get; }
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
    public List<Vector3> toMove;
    public List<Vector3> moved;
    public List<Vector3> matched;
    public List<string> unmatched;
    public int level;
    public List<ActionFrame> ActionQueue;
    public float speed = 5f;

    public int blocksAreMoving = 0;

    public Dictionary<Vector3, MatchBlocks> blockMap;
    public Dictionary<string, Vector3> matchMap;

    // Start is called before the first frame update
    void Start()
    {
        level = int.Parse(SceneManager.GetActiveScene().name.Substring(6));
        blockMap = new Dictionary<Vector3, MatchBlocks>();
        matchMap = new Dictionary<string, Vector3>();
        foreach (MatchBlocks b in blocks)
        {
            blockMap.Add(b.transform.position, b);
        }
        ActionQueue = new List<ActionFrame>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("r"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public int GetLevel() {
        return level;
    }

    public bool hasBlock(string blockName) {
        foreach (var item in blocks)
        {
            if (blockName == item.name)
            {
                return true;
            }
        }
        return false;
    }

    public Hits CollisionCheck(Vector3 pos, Vector3 dir)
    {
        Vector3 candidatePosition = pos + dir;
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

    public List<Vector3> MoveBlocks(Vector3 dir)
    {
        foreach (Vector3 block in new List<Vector3>(toMove))
        {
            moved.Add(blockMap[block].PushBlock(dir));
        }

        toMove.Clear();
        return moved;
    }

    public List<string> CheckForMatches()
    {
        List<string> matches = new List<string>();
        foreach (var block in new List<Vector3>(moved))
        {
            if (blockMap.ContainsKey(block))
            {
                matches.AddRange(MatchThree(block));
            }
        }
        moved.Clear();
        return matches;
    }

    public void UpdateBlock(Vector3 old, MatchBlocks neue)
    {
        blockMap.Remove(old);
        blockMap.Add(neue.GetXYZ(), neue);
    }

    int matchCount = 0;
    public List<string> MatchThree(Vector3 leader)
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

    void GetSameNeighbors(Vector3 home, string str)
    {
        if (blockMap.ContainsKey(home) && str == blockMap[home].GetSpriteName() && !matched.Contains(home))
        {
            matched.Add(home);
            matchCount++;
            GetSameNeighbors(home + Vector3.left, str);
            GetSameNeighbors(home + Vector3.right, str);
            GetSameNeighbors(home + Vector3.up, str);
            GetSameNeighbors(home + Vector3.down, str);
        }
    }

    public List<SwapPair> TrySwap(Vector3 cent)
    {
        List<SwapPair> pairs = new List<SwapPair>();
        pairs.AddRange(SwapHelper(cent, Vector3.left));
        pairs.AddRange(SwapHelper(cent, Vector3.up));
        pairs.AddRange(SwapHelper(cent, Vector3.right));
        pairs.AddRange(SwapHelper(cent, Vector3.down));
        return pairs;
    }

    public List<SwapPair> SwapHelper(Vector3 cent, Vector3 dir)
    {
        List<SwapPair> pairs = new List<SwapPair>();
        Vector3 close = cent + dir;
        Vector3 far = close + dir;

        if (blockMap.ContainsKey(far) && blockMap.ContainsKey(close))
        {
            pairs.Add(Swap(close, far));
            moved.Add(close);
            moved.Add(far);
        }
        return pairs;
    }

    public SwapPair Swap(Vector3 close, Vector3 far)
    {
        MatchBlocks temp = blockMap[far];
        blockMap[far] = blockMap[close];
        blockMap[close] = temp;
        blockMap[close].SetXYZ(close);
        blockMap[far].SetXYZ(far);
        return new SwapPair(far, close);
    }

    public void ChangeVisibilityOfMatchBlocks()
    {
        foreach (Vector3 b in matched)
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

    public void AddActionFrame(ActionFrame af)
    {
        ActionQueue.Add(af);
    }

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
            Vector3 undoDir = step.PlayerDir * -1;
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
                Swap(pair.CloseBlock,pair.FarBlock);
            }
            // Remove ActionFrame from ActionQueue
            ActionQueue.RemoveAt(ActionQueue.Count - 1);
        }
    }

    
    public float getSpeed() {
        return speed;
    }
}