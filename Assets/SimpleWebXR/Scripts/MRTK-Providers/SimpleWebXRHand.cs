﻿using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Rufus31415.WebXR;
using System.Collections.Generic;
using UnityEngine;

namespace Rufus31415.MixedReality.Toolkit.WebXR.Input
{

    [MixedRealityController(SupportedControllerType.ArticulatedHand, new[] { Handedness.Left, Handedness.Right })]
    public class SimpleWebXRHand : BaseHand
    {
        public SimpleWebXRHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions)
        {

        }

        #region IMixedRealityHand Implementation

        protected readonly Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();
        /// <inheritdoc/>
        public override bool TryGetJoint(TrackedHandJoint joint, out MixedRealityPose pose)
        {
            return jointPoses.TryGetValue(joint, out pose);
        }

        #endregion IMixedRealityHand Implementation


        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer, new MixedRealityInputAction(4, "Pointer Pose", AxisType.SixDof)),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip, new MixedRealityInputAction(3, "Grip Pose", AxisType.SixDof)),
            new MixedRealityInteractionMapping(2, "Select", AxisType.Digital, DeviceInputType.Select, new MixedRealityInputAction(1, "Select", AxisType.Digital)),
            new MixedRealityInteractionMapping(3, "Grab", AxisType.SingleAxis, DeviceInputType.TriggerPress, new MixedRealityInputAction(7, "Grip Press", AxisType.SingleAxis)),
            new MixedRealityInteractionMapping(4, "Index Finger Pose", AxisType.SixDof, DeviceInputType.IndexFinger,  new MixedRealityInputAction(13, "Index Finger Pose", AxisType.SixDof)),
    };

        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => DefaultInteractions;

        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => DefaultInteractions;

        public override void SetupDefaultInteractions()
        {
            AssignControllerMappings(DefaultInteractions);
        }

        public override bool IsInPointingPose => true;

        public void UpdateController(WebXRInput controller)
        {
            if (!Enabled) return;

            IsPositionAvailable = IsRotationAvailable = controller.Hand.Available;


            for (int i = 0; i < WebXRHand.JOINT_COUNT; i++)
            {
                var joint = controller.Hand.Joints[i];
                jointPoses[(TrackedHandJoint)(i + 1)] = new MixedRealityPose(joint.Position, joint.Rotation);
            }

            var indexJoint = controller.Hand.Joints[WebXRHand.INDEX_PHALANX_TIP];
            var indexPose = new MixedRealityPose(indexJoint.Position, indexJoint.Rotation);

            bool isSelecting;
            MixedRealityPose pointerPose;
            MixedRealityPose currentGripPose;


            if (controller.IsPositionTracked)
            {
                isSelecting = controller.Selected;
                pointerPose = new MixedRealityPose(controller.Position, controller.Rotation);
                currentGripPose = pointerPose;
            }
            else
            {
                isSelecting = Vector3.Distance(controller.Hand.Joints[WebXRHand.THUMB_PHALANX_TIP].Position, controller.Hand.Joints[WebXRHand.INDEX_PHALANX_TIP].Position) < 0.04;

                currentGripPose = jointPoses[TrackedHandJoint.Wrist];

                HandRay.Update((controller.Hand.Joints[WebXRHand.THUMB_PHALANX_TIP].Position + controller.Hand.Joints[WebXRHand.INDEX_PHALANX_TIP].Position) / 2, new Vector3(0.3f, -0.4f, 0.9f), CameraCache.Main.transform, ControllerHandedness);

                Ray ray = HandRay.Ray;

                pointerPose = new MixedRealityPose(ray.origin, Quaternion.LookRotation(ray.direction));
            }

            CoreServices.InputSystem?.RaiseSourcePoseChanged(InputSource, this, pointerPose);

            CoreServices.InputSystem?.RaiseHandJointsUpdated(InputSource, ControllerHandedness, jointPoses);

            UpdateVelocity();

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.SpatialPointer:
                        Interactions[i].PoseData = pointerPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, Interactions[i].PoseData);
                        }
                        break;
                    case DeviceInputType.SpatialGrip:
                        Interactions[i].PoseData = currentGripPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, Interactions[i].PoseData);
                        }
                        break;
                    case DeviceInputType.Select:
                        Interactions[i].BoolData = isSelecting;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.TriggerPress:
                        Interactions[i].BoolData = isSelecting;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.IndexFinger:
                        Interactions[i].PoseData = indexPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, Interactions[i].PoseData);
                        }
                        break;
                }
            }
        }
    }
}