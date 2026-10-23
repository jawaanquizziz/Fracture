using System;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    /// <summary>Distance-driven gait for the supplied rigs, including replicated and AI avatars.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class CharacterLocomotion : MonoBehaviour
    {
        private sealed class Joint
        {
            public Transform bone;
            public Quaternion rest;
            public Joint(Transform value) { bone=value; rest=value.localRotation; }
        }
        private Joint leftHip, rightHip, leftKnee, rightKnee, leftArm, rightArm, leftElbow, rightElbow;
        private Transform visual, leftFoot, rightFoot;
        private Vector3 visualRest, previousPosition;
        private Quaternion visualRestRotation = Quaternion.identity;
        private float phase, speed, weight, stillTime, footRest;
        public bool HasLegRig => leftHip!=null && rightHip!=null && leftKnee!=null && rightKnee!=null;
        public float GaitWeight => weight;
        public float GaitPhase => phase;

        public void Initialize(Transform model)
        {
            visual=model; visualRest=model.localPosition; visualRestRotation=model.localRotation; previousPosition=transform.position;
            Transform[] bones=model.GetComponentsInChildren<Transform>(true);
            Transform Find(string prefix) => bones.FirstOrDefault(b=>b.name.StartsWith(prefix,StringComparison.Ordinal) &&
                b.name.Substring(prefix.Length).All(char.IsDigit));
            Joint Bind(string name) { Transform b=Find(name); return b!=null?new Joint(b):null; }
            leftHip=Bind("L_Hip_"); rightHip=Bind("R_Hip_");
            leftKnee=Bind("L_Knee_"); rightKnee=Bind("R_Knee_");
            leftFoot=Find("L_Foot_"); rightFoot=Find("R_Foot_");
            // Relax imported A-pose arms before defining the gait's neutral pose.
            RelaxArm(Find("L_Shoulder_"),Find("L_Elbow_"),-1);
            RelaxArm(Find("R_Shoulder_"),Find("R_Elbow_"),1);
            leftArm=Bind("L_Shoulder_"); rightArm=Bind("R_Shoulder_");
            leftElbow=Bind("L_Elbow_"); rightElbow=Bind("R_Elbow_");
            if(leftFoot!=null && rightFoot!=null) footRest=Mathf.Min(leftFoot.position.y,rightFoot.position.y)-transform.position.y;
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen=true;
            if(!HasLegRig) Debug.LogWarning("[VALMON] Missing walking joints on "+model.name);
        }
        private void RelaxArm(Transform shoulder, Transform elbow, float side)
        {
            if(shoulder==null || elbow==null) return;
            Vector3 target=(-transform.up+transform.right*side*.12f).normalized;
            shoulder.rotation=Quaternion.FromToRotation((elbow.position-shoulder.position).normalized,target)*shoulder.rotation;
        }
        private void Swing(Joint joint, float degrees)
        {
            if(joint==null) return;
            Vector3 axis=joint.bone.parent.InverseTransformDirection(transform.right).normalized;
            joint.bone.localRotation=Quaternion.AngleAxis(degrees,axis)*joint.rest;
        }
        private void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if(visual==null || dt<=0) return;
            Vector3 delta=transform.position-previousPosition; previousPosition=transform.position; delta.y=0;
            // Respawns and snapshot teleports are not strides.
            if(delta.magnitude>2f) { speed=0; weight=0; stillTime=1; }
            else
            {
                stillTime=delta.sqrMagnitude>.000001f?0:stillTime+dt;
                float measured=Mathf.Min(16,delta.magnitude/dt);
                speed=Mathf.Lerp(speed,measured,1-Mathf.Exp(-12*dt));
            }
            float target=stillTime>.16f?0:Mathf.Clamp01(speed/.9f);
            weight=Mathf.MoveTowards(weight,target,dt*7);
            phase=Mathf.Repeat(phase+Mathf.Min(speed,9)*dt*2.5f,Mathf.PI*2);
            float stride=Mathf.Sin(phase), run=Mathf.InverseLerp(5,8,speed);
            float swing=Mathf.Lerp(30,44,run)*weight;
            if(HasLegRig)
            {
                // Full skeletal gait when the model has the expected bones.
                Swing(leftHip,-stride*swing); Swing(rightHip,stride*swing);
                Swing(leftKnee,Mathf.Max(0,Mathf.Cos(phase))*Mathf.Lerp(48,72,run)*weight);
                Swing(rightKnee,Mathf.Max(0,-Mathf.Cos(phase))*Mathf.Lerp(48,72,run)*weight);
                Swing(leftArm,stride*swing*.72f); Swing(rightArm,-stride*swing*.72f);
                Swing(leftElbow,-(12+run*30)*weight); Swing(rightElbow,-(12+run*30)*weight);
                visual.localPosition=visualRest;
                if(leftFoot!=null && rightFoot!=null)
                {
                    float lift=footRest-(Mathf.Min(leftFoot.position.y,rightFoot.position.y)-transform.position.y);
                    visual.position+=Vector3.up*Mathf.Clamp(lift,-.28f,.28f)*weight;
                }
                return;
            }
            // Fallback: unrigged character — Pokemon-GO-style procedural step.
            // Vertical hop (both feet "touch" when |sin phase|==0), slight side sway and forward lean.
            float hop=Mathf.Abs(Mathf.Sin(phase))*Mathf.Lerp(.09f,.17f,run)*weight;
            float sway=stride*Mathf.Lerp(4.5f,7f,run)*weight;
            float lean=Mathf.Lerp(3f,8f,run)*weight;
            visual.localPosition=visualRest+Vector3.up*hop;
            // Multiply onto the model's rest rotation so the catalog yaw-offset is preserved.
            visual.localRotation=visualRestRotation*Quaternion.Euler(lean,0f,-sway);
        }
    }
}
