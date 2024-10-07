using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Control")]
    public Camera PlayerCam;
    public Rigidbody RB;
    public Transform GunOrigin;
    public CapsuleCollider CCollider;
    public float MoveSpeed = 5;
    [Header("Physic Materials")]
    public PhysicMaterial Moving;
    public PhysicMaterial Stopping;
    [Range(-85, 85)]
    private float CameraRotation = 0;

    [Header("Mouse Sensitivty Options")]
    [Range(1,100)]
    public float MouseHorizontalSensitivity=1;
    [Range(1, 100)]
    public float MouseVerticalSensitivity=1;

    [Header("Shooting")]
    public AudioSource _AudioSource;
    public GameObject BulletHoleDecal;
    public float MinDamage = 10;
    public float MaxDamage = 20;
    public static List<saveray> myrays = new List<saveray>();

    // Start is called before the first frame update
    void Start()
    {
        if (RB == null) { RB = GetComponent<Rigidbody>(); }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


    }

    // Update is called once per frame
    void Update()
    {
        

        if (Input.GetButtonDown("Fire1")) 
        {
            Shoot();
        }
        //Debug.DrawRay(PlayerCam.transform.position, transform.forward);
        Ray ray = new(PlayerCam.transform.position, PlayerCam.transform.forward);
        RaycastHit FirstRay;
        Debug.DrawRay(PlayerCam.transform.position, PlayerCam.transform.forward, Color.blue);
        if (Physics.Raycast(ray, out FirstRay))
        {
            

            Ray ray2 = new Ray(GunOrigin.position, -Vector3.Normalize(GunOrigin.position - FirstRay.point));
            RaycastHit SecondRay;

            if (Physics.Raycast(ray2, out SecondRay))
            {
                Debug.DrawLine(GunOrigin.position, SecondRay.point, Color.green);
            }

        }

        foreach (saveray sr in myrays) 
        {
            Debug.DrawLine(sr.start, sr.End, sr.MyColor);
        }

    }

    private void FixedUpdate()
    {
        //transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, , 0));
        RB.MoveRotation(Quaternion.Euler(new Vector3(0, RB.rotation.eulerAngles.y + Input.GetAxis("Mouse X") *Time.fixedDeltaTime * MouseHorizontalSensitivity, 0)));

        

        CameraRotation -= Input.GetAxis("Mouse Y");
        if (CameraRotation < -85) { CameraRotation = -85; }
        if (CameraRotation > 85) { CameraRotation = 85; }
        PlayerCam.transform.rotation = Quaternion.Euler(new Vector3(CameraRotation, PlayerCam.transform.rotation.eulerAngles.y, PlayerCam.transform.rotation.eulerAngles.z));

        bool isMoving = (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0);
        if (isMoving)
        {
            CCollider.material = Moving;
        }
        else
        {
            CCollider.material = Stopping;
        }

        RB.AddRelativeForce(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")) * MoveSpeed * Time.fixedDeltaTime);
        
        
        
        //transform.rotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, 0));

    }

    private void Shoot() 
    {
        _AudioSource.Play();
        //Raycast from center of screen
        Ray ray = new(PlayerCam.transform.position, PlayerCam.transform.forward);
        RaycastHit FirstRay;
        if (Physics.Raycast(ray, out FirstRay))
        {


            //Debug.Log($"First Ray hit {FirstRay.transform.name}");
            Vector3 ShotDirection = -Vector3.Normalize(GunOrigin.position - FirstRay.point);
            Ray ray2 = new Ray(GunOrigin.position, ShotDirection);
            //RaycastHit SecondRay;
            RaycastHit[] SecondRay = Physics.RaycastAll(ray2, 100f); 
            float penetration = 0;
            if ( SecondRay.Length > 2) 
            {
                SecondRay = SortedRaycastAllArray(SecondRay, transform.position);
            }
            
            foreach( RaycastHit rh in SecondRay ) { Debug.Log($"Order{rh.collider.name}"); }

            saveray sr = new();
            sr.start = GunOrigin.position;
            sr.End = SecondRay[SecondRay.Length - 1].point;
            sr.MyColor = Color.red;
            myrays.Add(sr);
            
            for (int i = 0;i < SecondRay.Length; i++ )
            {
                Debug.Log($"Hit object {SecondRay[i].transform.gameObject.name} at point {SecondRay[i].point}");

                if (i + 1 < SecondRay.Length)
                {
                    Ray Ray3 = new Ray(SecondRay[i + 1].point, -ray2.direction);
                    RaycastHit TertiaryRay;
                    if (Physics.Raycast(Ray3, out TertiaryRay))
                    {
                        if (TertiaryRay.collider == SecondRay[i].collider)
                        {
                            Component[] hitComponents = TertiaryRay.transform.GetComponents(typeof(IShootable));
                            foreach (IShootable e in hitComponents)
                            {
                                e.OnShot(Random.Range(MinDamage, MaxDamage));
                            }

                            saveray myray = new()
                            {
                                start = SecondRay[i+1].point,
                                End = TertiaryRay.point,
                                MyColor = Color.green
                            };
                            Debug.Log($" {myray.start} {myray.End}");
                            myrays.Add(myray);

                            penetration += Vector3.Distance(SecondRay[i].point, TertiaryRay.point);
                            Debug.Log($"Measured total penetration of {penetration} at {TertiaryRay.transform.gameObject.name}");
                        }
                        else
                        {
                            Debug.Log("Sanity check failed for tertiary ray on secondary ray.");
                            Debug.Log($"Ray Two hit {SecondRay[i].transform.name}, Tertiary ray hit {TertiaryRay.transform.name}");
                        }

                    }
                    else
                    {
                        Debug.Log("Something tragic happened in the raycast code please fix");
                    }
                }
                else 
                {
                    Debug.Log("Nothing to draw back from");
                }

                GameObject ShotProj = GameObject.Instantiate(BulletHoleDecal, SecondRay[i].point +  (SecondRay[i].normal *.01f ) , new Quaternion());
                ShotProj.transform.forward = -SecondRay[i].normal;
            }

            /*
            if (Physics.Raycast(ray2, out SecondRay)) 
            {
                Debug.Log($"Second Ray Hit: {SecondRay.transform.name}");

                Component[] hitComponents = SecondRay.transform.GetComponents(typeof(IShootable));
                foreach (IShootable e in hitComponents) 
                {
                    e.OnShot(Random.Range(MinDamage,MaxDamage));
                }
                float distance = Vector3.Distance(GunOrigin.position, SecondRay.point);

            }
            */

        }



    }

    public RaycastHit[] SortedRaycastAllArray(RaycastHit[] raycastHits, Vector3 PlayerPos) 
    {
        int Len = raycastHits.Length;

        for (int i = 1; i < Len; ++i) 
        {
            float Key = Vector3.Distance(PlayerPos, raycastHits[i].point);
            RaycastHit KeyHit = raycastHits[i];
            int j = i - 1;
            float JDist = Vector3.Distance(PlayerPos, raycastHits[j].point);
            while (j >= 0 && JDist > Key) 
            {
                raycastHits[j+1] = raycastHits[j];
                j--;
                JDist = Vector3.Distance(PlayerPos, raycastHits[j].point);
            }
            raycastHits[j + 1] = KeyHit;
        }


        return raycastHits;
    }

}


public struct saveray 
{
    public Vector3 start;
    public Vector3 End;
    public Color MyColor;
}