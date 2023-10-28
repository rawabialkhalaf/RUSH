using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody rb;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Þã ÈÊÍÏíË ÇáÞíãÉ ÇáãäÇÓÈÉ ááãÚÇãáÇÊ "IsMoving" æ "IsIdle" ÈäÇÁð Úáì ÍÑßÉ ÇááÇÚÈ
        bool isMoving = rb.velocity.magnitude > 0.1f;
        anim.SetBool("IsMoving", isMoving);
        anim.SetBool("IsIdle", !isMoving);
    }
}
