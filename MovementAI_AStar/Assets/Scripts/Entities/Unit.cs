using UnityEngine;

public class Unit : MonoBehaviour
{
    private Movement CurrentMovement;
    private bool IsSelected = false;
    private Material MaterialRef;
    private Color BaseColor = Color.white;

    protected void Awake()
    {
        MaterialRef = GetComponent<Renderer>().material;
        BaseColor = MaterialRef.color;
        CurrentMovement = GetComponent<Movement>();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        MaterialRef.color = IsSelected ? Color.red : BaseColor;
    }

    public void SetTargetPos(Vector3 pos)
    {
        CurrentMovement.TargetPos = pos;
    }
}
