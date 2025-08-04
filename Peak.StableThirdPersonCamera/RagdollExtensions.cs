using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Linkoid.Peak.StableCamera;

internal static class RagdollExtensions
{
    public static Vector3 CenterOfMass(this CharacterRagdoll ragdoll)
    {
        Vector3 positionSum = Vector3.zero;
        float massSum = 0.0f;
        foreach (var bodypart in ragdoll.partList)
        {
            var rig = bodypart.Rig;
            positionSum += bodypart.transform.TransformPoint(rig.centerOfMass) * rig.mass;
            massSum += rig.mass;
        }

        return positionSum / massSum;
    }
}
