using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public bool CanBePossessed => true;

    private bool isPossessed = false;

    public void OnPossessed()
    {
        isPossessed = true;
        Debug.Log($"{name} poseído");
    }

    public void OnReleased()
    {
        isPossessed = false;
        Debug.Log($"{name} liberado");
    }

    public bool IsPossessed => isPossessed;
}
