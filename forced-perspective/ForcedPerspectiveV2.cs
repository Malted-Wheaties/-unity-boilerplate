using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ForcedPerspectiveV2 : MonoBehaviour
{
    [SerializeField] private Image crosshair;
    [SerializeField] private Transform targetForCurrentObjects;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask raycastIgnoreMask;
    
    private GameObject currentObject;
    private float distanceMultiplier;
    private Vector3 scaleMultiplier;
    private Vector3 lastRotation;
    private float lastRotationY;
    private float currentObjectSize;
    private int currentObjectSizeIndex;
    private Vector3 centreCorrection;
    private Ray ray;
    private float cosine;
    private float cameraHeight;
    private float positionCalculation;
    private float raycastMaxRange = 1000f;
    private float lastPositionCalculation;
    private Vector3 lastHitPoint;
    
    private void Update()
    {
        ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, (Screen.height / 2) + (Screen.height / 10), 0));
        Debug.DrawRay(ray.origin, ray.direction * 200, Color.yellow);
        
        RaycastHit hit;
        bool rayHitSomething = Physics.Raycast(transform.position, transform.forward, out hit, raycastMaxRange, ~raycastIgnoreMask);
        print(rayHitSomething);
        print(hit.transform.name);
        print(hit.transform.tag);
        if (rayHitSomething) crosshair.color = hit.transform.CompareTag("Getable") ? Color.blue : Color.yellow;

        if (currentObject != null) crosshair.color = Color.red;
        else targetForCurrentObjects.position = hit.point;
        
        if (Input.GetMouseButtonDown(0) && rayHitSomething)
        {
            if (hit.transform.CompareTag("Getable"))
            {
            print("W");
                currentObject = hit.transform.gameObject;

                distanceMultiplier = Vector3.Distance(transform.position, currentObject.transform.position);
                scaleMultiplier = currentObject.transform.localScale;
                lastRotation = currentObject.transform.rotation.eulerAngles;
                lastRotationY = lastRotation.y - transform.eulerAngles.y;
                currentObject.transform.parent = targetForCurrentObjects;
                
                Rigidbody rb;
                if (!currentObject.TryGetComponent(out rb)) {
                    rb = currentObject.AddComponent<Rigidbody>();
                }
                rb.isKinematic = true;

                foreach (Collider col in currentObject.GetComponents<Collider>())
                {
                    col.isTrigger = true;
                }
                
                MeshRenderer mr;
                if (!currentObject.TryGetComponent(out mr)) {
                    mr = currentObject.AddComponent<MeshRenderer>();
                }
                //mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //mr.receiveShadows = false;
                
                currentObject.gameObject.layer = 8;
                foreach (Transform child in currentObject.GetComponentsInChildren<Transform>())
                {
                    currentObject.GetComponent<Rigidbody>().isKinematic = true;
                    currentObject.GetComponent<Collider>().isTrigger = true;
                    child.gameObject.layer = 8;
                }
                
                Vector3 currentObjectColliderBoundSize = currentObject.GetComponent<Collider>().bounds.size;
                float[] currentObjectSizeArray =
                {
                    currentObjectColliderBoundSize.x, 
                    currentObjectColliderBoundSize.y,
                    currentObjectColliderBoundSize.z
                };
                
                float currentObjectSize = currentObjectSizeArray.Max();
                int takenObjSizeIndex = currentObjectSizeArray.ToList().IndexOf(currentObjectSize);
            }
        }

        if (Input.GetMouseButton(0))
        {
            // Recentre the object to the centre of the mesh regardless of the real pivot point
            if (currentObject != null)
            {
                //centreCorrection = currentObject.transform.position -
                                   //currentObject.GetComponent<MeshRenderer>().bounds.center;

                currentObject.transform.position = Vector3.Lerp(currentObject.transform.position,
                    targetForCurrentObjects.position + centreCorrection, Time.deltaTime * 5);
                
                currentObject.transform.rotation = Quaternion.Lerp(currentObject.transform.rotation, Quaternion.Euler(new Vector3(0, lastRotationY + mainCamera.transform.eulerAngles.y, 0)), Time.deltaTime * 5);

                cosine = Vector3.Dot(ray.direction, hit.normal);
                cameraHeight = Mathf.Abs(hit.distance * cosine);

                currentObjectSize = currentObject.GetComponent<Collider>().bounds.size[currentObjectSizeIndex];

                positionCalculation = (hit.distance * currentObjectSize / 2) / (cameraHeight);
                if (positionCalculation < raycastMaxRange)
                {
                    lastPositionCalculation = positionCalculation;
                }
                
                // If the wall is more distant than the raycast max range, increase the size only until the max range
                lastHitPoint = rayHitSomething
                    ? hit.point
                    : mainCamera.transform.position + ray.direction * raycastMaxRange;

                targetForCurrentObjects.position = Vector3.Lerp(targetForCurrentObjects.position, lastHitPoint
                    - (ray.direction * lastPositionCalculation), Time.deltaTime * 10);

                currentObject.transform.localScale = scaleMultiplier *
                                                     (Vector3.Distance(mainCamera.transform.position,
                                                         currentObject.transform.position) / distanceMultiplier);
                
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentObject != null)
            {
                currentObject.GetComponent<Rigidbody>().isKinematic = false;

                foreach (Collider col in currentObject.GetComponents<Collider>())
                {
                    col.isTrigger = false;
                }

                if (currentObject.GetComponent<MeshRenderer>() != null)
                {
                    MeshRenderer currentObjectMeshRender = currentObject.GetComponent<MeshRenderer>();
                    //currentObjectMeshRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    //currentObjectMeshRender.receiveShadows = true;
                }

                currentObject.transform.parent = null;
                currentObject.gameObject.layer = 0;

                foreach (var child in currentObject.GetComponentsInChildren<Transform>())
                {
                    currentObject.GetComponent<Rigidbody>().isKinematic = false;
                    currentObject.GetComponent<Collider>().isTrigger = false;
                    child.gameObject.layer = 0;
                }

                currentObject = null;
            }
        }
    }
}
