using UnityEngine;

public class InGameButtons : MonoBehaviour
{
    public GameplayManager gm;
    public PlayerController p;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Left()
    {
        p.SetButtonAction(2);
    }

    public void Right()
    {
        p.SetButtonAction(4);

    }

    public void Up()
    {
        p.SetButtonAction(1);

    }

    public void Down()
    {
        p.SetButtonAction(3);

    }

    public void Restart()
    {
        gm.Restart();
    }

    public void Undo()
    {
        p.SetButtonAction(6);
    }

    public void Swap()
    {
        p.SetButtonAction(5);
    }
}
