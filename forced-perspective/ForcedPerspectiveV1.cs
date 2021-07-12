using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEditorInternal;
using UnityEngine;

public class ForcedPerspectiveV1 : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform target;

    [Header("Parameters")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask ignoreTargetMask;
    [SerializeField] private float offsetFactor;
    
    // Private variables
    private float originalDistance;
    private float originalScale;
    private Vector3 targetScale;
    
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        ResizeTarget();
    }
    
    private void HandleInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (target is null)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, targetMask))
            {
                target = hit.transform;
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                target.GetComponent<Rigidbody>().isKinematic = true;
                originalDistance = Vector3.Distance(transform.position, target.position);
                Vector3 targetLocalScale = target.localScale; // More performant as repeated property access of a built-in component is inefficient
                originalScale = targetLocalScale.x;
                targetScale = targetLocalScale;
            }
        }
        else
        {
            target.GetComponent<Rigidbody>().isKinematic = false;
            target = null;
        }
    }

    private void ResizeTarget()
    {
        if (ReferenceEquals(target, null)) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ignoreTargetMask))
        {
            target.position = hit.point - transform.forward * (offsetFactor * targetScale.x);

            float currentDistance = Vector3.Distance(transform.position, target.position);
            float s = currentDistance / originalDistance;
            targetScale.x = targetScale.y = targetScale.z = s;

            target.localScale = targetScale * originalScale;
        }
    }
}
