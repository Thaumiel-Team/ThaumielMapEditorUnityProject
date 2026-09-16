using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Collab
{
    [DisallowMultipleComponent]
    public class CollabId : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        private string _id = string.Empty;

        private static readonly Dictionary<string, CollabId> _registry = new();

        public string GuidString
        {
            get
            {
                if (string.IsNullOrEmpty(_id))
                    EnsureAssigned();

                return _id;
            }
        }

        public Guid Guid
        {
            get
            {
                if (Guid.TryParse(GuidString, out Guid g))
                    return g;

                EnsureAssigned();
                return Guid.Parse(_id);
            }
        }

        private void Awake()
        {
            EnsureAssigned();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(_id))
                _registry[_id] = this;
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(_id) && _registry.TryGetValue(_id, out CollabId cur) && ReferenceEquals(cur, this))
                _registry.Remove(_id);
        }

        private void OnValidate()
        {
            EnsureAssigned();
        }

        public void EnsureAssigned()
        {
            if (!string.IsNullOrEmpty(_id) && Guid.TryParse(_id, out _))
                return;

            Regenerate();
        }

        public void Regenerate()
        {
            if (!string.IsNullOrEmpty(_id) && _registry.TryGetValue(_id, out CollabId cur) && ReferenceEquals(cur, this))
                _registry.Remove(_id);

            _id = Guid.NewGuid().ToString("N");
            _registry[_id] = this;
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ForceSet(string guidN)
        {
            if (!string.IsNullOrEmpty(_id) && _registry.TryGetValue(_id, out CollabId cur) && ReferenceEquals(cur, this))
                _registry.Remove(_id);

            _id = guidN;
            _registry[_id] = this;
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
        }

        public static string Get(GameObject go)
        {
            if (go == null)
                return string.Empty;

            CollabId c = go.GetComponent<CollabId>();
            return c != null ? c.GuidString : string.Empty;
        }

        public static string Ensure(GameObject go)
        {
            if (go == null)
                return string.Empty;

            CollabId c = go.GetComponent<CollabId>();
            c = c != null ? c : go.AddComponent<CollabId>();
            if (!string.IsNullOrEmpty(c._id) && _registry.TryGetValue(c._id, out CollabId other) && other != null && !ReferenceEquals(other, c))
            {
                c.Regenerate();
            }
            else
                c.EnsureAssigned();
                
            return c.GuidString;
        }

        public static GameObject Find(string guidN)
        {
            if (string.IsNullOrEmpty(guidN))
                return null;

            if (_registry.TryGetValue(guidN, out CollabId c) && c != null)
                return c.gameObject;

            CollabId[] all = FindObjectsByType<CollabId>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CollabId item in all)
            {
                if (item != null && item._id == guidN)
                {
                    _registry[guidN] = item;
                    return item.gameObject;
                }
            }
            return null;
        }
    }
}
