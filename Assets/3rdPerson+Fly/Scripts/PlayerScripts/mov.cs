using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 0.15f;
    public float runSpeed = 1.0f;
    public float sprintSpeed = 2.0f;
    public float speedDampTime = 0.1f;
    private float speed, speedSeeker;

    private Animator anim; // ãßæä Animator
    private Rigidbody rb;
    private bool isPlayerMoving = false; // ãÊÛíÑ ááÊÍŞŞ ãä ÍÇáÉ ÍÑßÉ ÇááÇÚÈ

    void Start()
    {
        anim = GetComponent<Animator>(); // ÇáÍÕæá Úáì ãßæä Animator
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // ÊÍÏíË ÍÇáÉ ÇáÇäãíÔä ÈäÇÁğ Úáì ÊÍÑß ÇááÇÚÈ
       
    }

    void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        MovementManagement(horizontal, vertical);
        anim.SetBool("walk", vertical > 0);
    }

    void MovementManagement(float horizontal, float vertical)
    {
        // ÊÍÏíË ÇáÍÑßÉ ÈäÇÁğ Úáì ÅÏÎÇáÇÊ áæÍÉ ÇáãİÇÊíÍ æÛíÑåÇ
        Vector3 movement = Quaternion.Euler(0,transform.eulerAngles.y,0) * new Vector3(0, 0f, vertical) * walkSpeed * Time.deltaTime; // ÊÍÏíÏ ÇáÍÏ ÇáÃŞÕì ááÓÑÚÉ
        movement *= (Input.GetKey(KeyCode.LeftShift)) ? sprintSpeed : (Input.GetKey(KeyCode.LeftControl)) ? walkSpeed : runSpeed;
        transform.Rotate(0, horizontal * 300f * Time.deltaTime, 0);
        // ÊÍÏíË ÇáÍÑßÉ
        rb.MovePosition(transform.position + movement);
    }
}
