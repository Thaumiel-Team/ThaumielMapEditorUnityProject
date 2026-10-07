using System.Collections.Generic;
using Assets.Scripts.Enums;
using UnityEngine;
using System;

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class Permission
    {
        [Header("Global:")]
        
        [Tooltip("The roles that are allowed to trigger this. Empty = all")]
        public List<RoleTypeId> AllowedRoles;

        public string LabAPIPermission;

        public DoorPermissionFlags KeycardPermissions;
    }
}