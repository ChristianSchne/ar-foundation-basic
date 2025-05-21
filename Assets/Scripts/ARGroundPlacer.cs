using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Handles AR ground plane detection and object placement
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class ARGroundPlacer : MonoBehaviour
{
    // Singleton instance
    public static ARGroundPlacer instance;
    
    // Serialized fields for inspector configuration
    [SerializeField] private Transform groundIndicator;
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform targetObject;
    
    // Maximum distance for raycast in meters
    private const float MaxRaycastDistance = 15f;
    
    // Public properties
    public Pose CurrentGroundPose { get; private set; } = Pose.identity;
    public ARRaycastHit CurrentHit { get; private set; }
    
    // Component references
    private ARRaycastManager arRaycastManager;

    /// <summary>
    /// Initialize the component and set up singleton pattern
    /// </summary>
    private void Awake()
    {
        // Implement singleton pattern
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // Get required component
        arRaycastManager = GetComponent<ARRaycastManager>();
    }

    /// <summary>
    /// Update ground indicator position and orientation based on AR plane detection
    /// </summary>
    private void LateUpdate()
    {
        UpdateGroundIndicatorRotation();
        UpdateGroundPosition();
    }
    
    /// <summary>
    /// Aligns the ground indicator with the camera's forward direction
    /// </summary>
    private void UpdateGroundIndicatorRotation()
    {
        // Make indicator face the same direction as camera (ignoring vertical component)
        Vector3 flatForward = new Vector3(arCamera.transform.forward.x, 0, arCamera.transform.forward.z);
        groundIndicator.transform.rotation = Quaternion.LookRotation(flatForward);
    }
    
    /// <summary>
    /// Updates the ground position based on AR raycast or fallback method
    /// </summary>
    private void UpdateGroundPosition()
    {
        // Get screen center point
        Vector3 screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0));
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        
        // Try to raycast to AR plane
        arRaycastManager.Raycast(screenCenter, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon);
        
        if (hits.Count > 0)
        {
            // AR plane detected
            CurrentHit = hits[0];
            CurrentGroundPose = hits[0].pose;
            groundIndicator.transform.position = CurrentGroundPose.position;
        }
        else
        {
            // Fallback when no AR plane is detected
            FallbackGroundPositioning();
        }
    }
    
    /// <summary>
    /// Fallback method to estimate ground position when no AR plane is detected
    /// </summary>
    private void FallbackGroundPositioning()
    {
        // Use last known ground plane for raycasting
        Plane groundPlane = new Plane(Vector3.up, CurrentGroundPose.position);
        Ray cameraRay = arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1));
        
        if (groundPlane.Raycast(cameraRay, out float enter))
        {
            // Limit maximum distance
            enter = Mathf.Min(MaxRaycastDistance, enter);
            Vector3 groundHit = cameraRay.GetPoint(enter);
            
            // Ensure point is on ground plane
            groundHit = groundPlane.ClosestPointOnPlane(groundHit);
            
            // Update pose
            CurrentGroundPose = new Pose(groundHit, Quaternion.LookRotation(groundPlane.normal));
            groundIndicator.transform.position = CurrentGroundPose.position;
        }
    }

    /// <summary>
    /// Places the target object at the current ground position
    /// </summary>
    public void PlaceObject()
    {
        if (CurrentGroundPose != Pose.identity)
        {
            // Place object on detected ground
            targetObject.position = CurrentGroundPose.position;
            
            // Orient object to face same direction as camera (horizontally)
            Vector3 cameraForward = arCamera.transform.forward;
            Vector3 cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            targetObject.rotation = Quaternion.LookRotation(cameraBearing);
        }
        else
        {
            // Fallback positioning when no ground detected
            targetObject.position = arCamera.transform.position + 
                                   arCamera.transform.forward * 2 - 
                                   Vector3.up * 1;
        }
    }
}