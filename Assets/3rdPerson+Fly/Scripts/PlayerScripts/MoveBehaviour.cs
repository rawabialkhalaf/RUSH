using UnityEngine;

public class MoveBehaviour : GenericBehaviour
{
    public float walkSpeed = 0.15f;
    public float runSpeed = 1.0f;
    public float sprintSpeed = 2.0f;
    public float speedDampTime = 0.1f;
    private float speed, speedSeeker;
    private int groundedBool;

    private Animator anim; // مكون Animator

    void Start()
    {
        groundedBool = Animator.StringToHash("Grounded");

        anim = GetComponent<Animator>(); // الحصول على مكون Animator
        anim.SetBool(groundedBool, true);

        behaviourManager.SubscribeBehaviour(this);
        behaviourManager.RegisterDefaultBehaviour(this.behaviourCode);
        speedSeeker = runSpeed;
    }

    public override void LocalFixedUpdate()
    {
        MovementManagement(behaviourManager.GetH, behaviourManager.GetV);
    }

    void MovementManagement(float horizontal, float vertical)
    {
        if (behaviourManager.IsGrounded())
            behaviourManager.GetRigidBody.useGravity = true;

      

        Vector2 dir = new Vector2(horizontal, vertical);
        speed = Vector2.ClampMagnitude(dir, 1f).magnitude;

        speedSeeker += Input.GetAxis("Mouse ScrollWheel");
        speedSeeker = Mathf.Clamp(speedSeeker, walkSpeed, runSpeed);
        speed *= speedSeeker;

        if (behaviourManager.IsSprinting())
        {
            speed = sprintSpeed;
        }

        anim.SetFloat("speed", speed, speedDampTime, Time.deltaTime); // تحديث قيمة المتغير في الرسوم المتحركة
    }

    //... باقي الكود
}
