﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class GrabControllerOculus : MonoBehaviour
{

    public GrabControllerOculus OtherHandReference;
    public XRNode NodeType;
    public float GrabDistance = 0.1f;
    public float ThrowMultiplier = 1.5f;

    public Transform CurrentGrabObject
    {
        get { return _currentGrabObject; }
        set { _currentGrabObject = value; }
    }

    private Vector3 _lastFramePosition;
    private Transform _currentGrabObject;
    private bool _isGrabbing;

    // Use this for initialization
    void Start()
    {
        _lastFramePosition = transform.position;

        XRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale);

        _currentGrabObject = null;

        _isGrabbing = false;

    }

    // Update is called once per frame
    void Update()
    {
        //update hand position and rotation
        transform.localPosition = InputTracking.GetLocalPosition(NodeType);
        transform.localRotation = InputTracking.GetLocalRotation(NodeType);


        //if we don't have an active object in hand, look if there is one in proximity
        if (_currentGrabObject == null)
        {
            //check for colliders in proximity
            Collider[] colliders = Physics.OverlapSphere(transform.position, GrabDistance);
            if (colliders.Length > 0)
            {
                //if there are colliders, take the first one if we press the grab button and it has the tag for grabbing
                if (CheckGripButton(InputButtonStates.DOWN) && colliders[0].GetComponent<GrabInteractiveItem>() != null)
                {
                    //if we are already grabbing, return
                    if (_isGrabbing)
                    {
                        return;
                    }
                    _isGrabbing = true;

                    //set current object to the object we have picked up (set it as child)
                    colliders[0].transform.SetParent(transform);

                    //if there is no rigidbody to the grabbed object attached, add one
                    if (colliders[0].GetComponent<Rigidbody>() == null)
                    {
                        colliders[0].gameObject.AddComponent<Rigidbody>();
                    }

                    //set grab object to kinematic (disable physics)
                    colliders[0].GetComponent<Rigidbody>().isKinematic = true;


                    //save a reference to grab object
                    _currentGrabObject = colliders[0].transform;

                    //does other hand currently grab object? then release it!
                    if (OtherHandReference.CurrentGrabObject != null)
                    {
                        OtherHandReference.CurrentGrabObject = null;
                    }
                }
            }
        }
        else
        //we have object in hand, update its position with the current hand position (+defined offset from it)
        {

            //if we we release grab button, release current object
            if (CheckGripButton(InputButtonStates.UP))
            {

                //set grab object to non-kinematic (enable physics)
                Rigidbody _objectRGB = _currentGrabObject.GetComponent<Rigidbody>();
                _objectRGB.isKinematic = false;
                _objectRGB.collisionDetectionMode = CollisionDetectionMode.Continuous;

                //calculate the hand's current velocity
                Vector3 CurrentVelocity = (transform.position - _lastFramePosition) / Time.deltaTime;

                //set the grabbed object's velocity to the current velocity of the hand
                _objectRGB.velocity = CurrentVelocity * ThrowMultiplier;

                //release the the object (unparent it)
                _currentGrabObject.SetParent(null);

                //release reference to object
                _currentGrabObject = null;
            }

        }

        //release grab ?
        if (CheckGripButton(InputButtonStates.UP))
        {
            _isGrabbing = false;
        }

        //save the current position for calculation of velocity in next frame
        _lastFramePosition = transform.position;


    }

    private bool CheckGripButton(InputButtonStates state)
    {
        if (NodeType == XRNode.LeftHand)
            return ModuleInput.Instance.GetSelectionMethodRift().AuxiliarGripButton(state);

        else if (NodeType == XRNode.RightHand)
            return ModuleInput.Instance.GetSelectionMethodRift().MainGripButton(state);

        else
            Debug.Log("XRNode type mus be a Hand!  Current type is" + NodeType.ToString());

        return false;
    }
}