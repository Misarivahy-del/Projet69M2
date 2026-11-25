using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
using System;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// NetworkInteractableBase class synchronizes the <see cref="XRBaseInteractable"/> events over the network.
    /// Options are exposed to determine which functionality you want to synchronize.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    [DisallowMultipleComponent]
    public class NetworkBaseInteractable : NetworkBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        public bool allowOverrideOwnership
        {
            get => m_AllowOverrideOwnership;
            set => m_AllowOverrideOwnership = value;
        }

        [Header("General Options"), SerializeField, Tooltip("Allow users to take ownership of currently controlled objects")]
        protected bool m_AllowOverrideOwnership = false;

        public float interactionCheckTime
        {
            get => m_InteractionCheckTime;
            set => m_InteractionCheckTime = value;
        }
        [SerializeField, Tooltip("Amount of time before checking for false positives of the object interaction state.")]
        protected float m_InteractionCheckTime = 2.0f;

        public bool ignoreSocketSelectedCallback
        {
            get => m_IgnoreSocketSelectedCallback;
            set => m_IgnoreSocketSelectedCallback = value;
        }
        [SerializeField, Tooltip("Ignore Socket Interaction")]
        protected bool m_IgnoreSocketSelectedCallback = true;

        public bool resetObjectOnDisconenct
        {
            get => m_ResetObjectOnDisconnect;
            set => m_ResetObjectOnDisconnect = value;
        }
        [SerializeField, Tooltip("Reset object on disconnect")]
        protected bool m_ResetObjectOnDisconnect = true;

        public bool relinquishOwnershipAfterTime
        {
            get => m_RelinquishOwnershipAfterTime;
            set => m_RelinquishOwnershipAfterTime = value;
        }
        [Header("Ownership Relinquish"), SerializeField, Tooltip("Should we relinquish ownership back to the room host after a set amount of time?")]
        protected bool m_RelinquishOwnershipAfterTime = true;

        public float relinquishOwnershipTime
        {
            get => m_RelinquishOwnershipTime;
            set => m_RelinquishOwnershipTime = value;
        }
        [SerializeField, Tooltip("Amount of time before relinquishing ownership of the object back to the host.")]
        protected float m_RelinquishOwnershipTime = 5.0f;

        public bool isInteracting
        {
            get => m_IsInteracting.Value;
        }

        protected NetworkVariable<bool> m_IsInteracting = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [HideInInspector, SerializeField, Tooltip("Use a Unity Event for a callback when the IsInteracting value changes.")]

#pragma warning disable 0414
        bool m_UseInteractingChangedEvent = false;
#pragma warning restore 0414
        [HideInInspector] public UnityEvent<bool> OnInteractingChanged;

        public bool syncHover
        {
            get => m_SyncHover;
            set => m_SyncHover = value;
        }
        [HideInInspector, SerializeField, Tooltip("Sync Hover interaction over network")]
        protected bool m_SyncHover = false;
        [HideInInspector, SerializeField, Tooltip("Use Unity Events for Networked Hooks")]
        protected bool m_UseHoverEvents = false;
        [HideInInspector] public UnityEvent<bool> HoverNetworkedEventServer;
        [HideInInspector] public UnityEvent<bool> HoverNetworkedEventAll;

        public bool syncSelect
        {
            get => m_SyncSelect;
            set => m_SyncSelect = value;
        }
        [HideInInspector, SerializeField, Tooltip("Sync Select interaction over network")]
        protected bool m_SyncSelect = true;
        [HideInInspector, SerializeField, Tooltip("Use Unity Events for Networked Hooks")]
        protected bool m_UseSelectEvents = false;
        [HideInInspector] public UnityEvent<bool> SelectNetworkedEventServer;
        [HideInInspector] public UnityEvent<bool> SelectNetworkedEventAll;

        public bool syncActivate
        {
            get => m_SyncActivate;
            set => m_SyncActivate = value;
        }
        [HideInInspector, SerializeField, Tooltip("Sync Activate interaction over network")]
        protected bool m_SyncActivate = true;
        [HideInInspector, SerializeField, Tooltip("Use Unity Events for Networked Hooks")]
        protected bool m_UseActivateEvents = false;
        [HideInInspector] public UnityEvent<bool> ActivateNetworkedEventServer;
        [HideInInspector] public UnityEvent<bool> ActivateNetworkedEventAll;

        public XRBaseInteractable baseInteractable
        {
            get => m_BaseInteractable;
            set => m_BaseInteractable = value;
        }
        protected XRBaseInteractable m_BaseInteractable;

        public bool canProcess => isActiveAndEnabled;

        protected Pose m_OriginalPose;
        protected Vector3 m_OriginalScale;
        protected XRInteractionManager m_InteractionManager;

#pragma warning disable CS0618
        protected BaseAffordanceStateProvider m_AffordanceStateProvider;
#pragma warning restore CS0618

#if UNITY_EDITOR
        [HideInInspector, SerializeField]
        bool[] m_FoldoutValues = { true, true, true };
#endif

        IEnumerator m_RelinquishToHostEnumerator;
        IEnumerator m_HostInteractionCheckEnumerator;

        public virtual void Awake()
        {
            if (!TryGetComponent(out m_BaseInteractable))
            {
                Utils.Log("Missing Components! Disabling Now.", 2);
                enabled = false;
                return;
            }

            m_BaseInteractable.selectFilters.Add(this);
            m_BaseInteractable.hoverFilters.Add(this);

            m_InteractionManager = FindFirstObjectByType<XRInteractionManager>();

#pragma warning disable CS0618
            m_AffordanceStateProvider = GetComponentInChildren<BaseAffordanceStateProvider>();
#pragma warning restore CS0618
        }

        private void OnEnable()
        {
            m_OriginalPose.position = transform.position;
            m_OriginalPose.rotation = transform.rotation;
            m_OriginalScale = transform.localScale;

            SetupListeners(true);
        }

        private void OnDisable()
        {
            SetupListeners(false);
        }

        void SetupListeners(bool setup)
        {
            if (setup)
            {
                if (baseInteractable != null)
                {
                    baseInteractable.hoverEntered.AddListener(OnHoverEnterLocal);
                    baseInteractable.hoverExited.AddListener(OnHoverExitLocal);
                    baseInteractable.selectEntered.AddListener(OnSelectEnteredLocal);
                    baseInteractable.selectExited.AddListener(OnSelectExitedLocal);
                    baseInteractable.activated.AddListener(OnActivateLocal);
                    baseInteractable.deactivated.AddListener(OnDeactivateLocal);
                }
            }
            else
            {
                if (baseInteractable != null)
                {
                    baseInteractable.hoverEntered.RemoveListener(OnHoverEnterLocal);
                    baseInteractable.hoverExited.RemoveListener(OnHoverExitLocal);
                    baseInteractable.selectEntered.RemoveListener(OnSelectEnteredLocal);
                    baseInteractable.selectExited.RemoveListener(OnSelectExitedLocal);
                    baseInteractable.activated.RemoveListener(OnActivateLocal);
                    baseInteractable.deactivated.RemoveListener(OnDeactivateLocal);
                }
            }
        }

        private void SetOwnershipLockSafe(bool locked)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.NetworkConfig.NetworkTopology == NetworkTopologyTypes.DistributedAuthority)
            {
                NetworkObject.SetOwnershipLock(locked);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetworkObject.DontDestroyWithOwner = true;
            m_IsInteracting.OnValueChanged += OnIsInteractingChanged;

            if (IsOwner)
                m_IsInteracting.Value = false;

            if (m_ResetObjectOnDisconnect)
                ResetObject();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_IsInteracting.OnValueChanged -= OnIsInteractingChanged;

            if (IsOwner)
                m_IsInteracting.Value = false;

            if (m_ResetObjectOnDisconnect)
                ResetObject();
        }

        public virtual void ResetObject()
        {
            transform.SetPositionAndRotation(m_OriginalPose.position, m_OriginalPose.rotation);
            transform.localScale = m_OriginalScale;
        }

        public virtual void OnHoverEnterLocal(BaseInteractionEventArgs args)
        {
            if (!IsSpawned) return;

            if (m_IgnoreSocketSelectedCallback && args.interactorObject.transform.GetComponent<XRSocketInteractor>() != null)
                return;

            Hovered(true);
            if (syncHover)
                OnHoverOwnerRpc(true, NetworkManager.Singleton.LocalClientId);
        }

        public virtual void OnHoverExitLocal(BaseInteractionEventArgs args)
        {
            if (!IsSpawned) return;

            Hovered(false);
            if (syncHover)
                OnHoverOwnerRpc(false, NetworkManager.Singleton.LocalClientId);
        }

        [Rpc(SendTo.Owner)]
        public virtual void OnHoverOwnerRpc(bool entered, ulong clientId)
        {
            OnHoverRpc(entered, clientId);
            if (m_UseHoverEvents)
                HoverNetworkedEventServer.Invoke(entered);
        }

        [Rpc(SendTo.Everyone)]
        public virtual void OnHoverRpc(bool entered, ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                Hovered(entered);

#pragma warning disable CS0618
                m_AffordanceStateProvider?.UpdateAffordanceState(new AffordanceStateData(Convert.ToByte(entered ? 2 : (isInteracting ? 4 : 0)), 1.0f));
#pragma warning restore CS0618
            }
        }

        public virtual void Hovered(bool entered)
        {
            if (m_UseHoverEvents)
                HoverNetworkedEventAll.Invoke(entered);
        }

        public virtual void OnSelectEnteredLocal(BaseInteractionEventArgs args)
        {
            if (!IsSpawned) return;

            if (m_IgnoreSocketSelectedCallback && args.interactorObject.transform.GetComponent<XRSocketInteractor>() != null)
                return;

            if (CanHold())
            {
                Selected(true);
                if (syncSelect)
                {
                    if (NetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer)
                        OnSelectServerRpc(true, NetworkManager.Singleton.LocalClientId);
                    else
                        OnSelectOwnerRpc(true, NetworkManager.Singleton.LocalClientId);
                }

                if (IsOwner)
                {
                    m_IsInteracting.Value = true;
                    SetOwnershipLockSafe(true);
                }
            }
        }

        public virtual void OnSelectExitedLocal(BaseInteractionEventArgs args)
        {
            if (!IsSpawned) return;

            if (m_IgnoreSocketSelectedCallback && args.interactorObject.transform.GetComponent<XRSocketInteractor>() != null)
                return;

            if (m_BaseInteractable.isSelected)
                return;

            Selected(false);

            if (syncSelect)
            {
                if (NetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer)
                    OnSelectServerRpc(false, NetworkManager.Singleton.LocalClientId);
                else
                    OnSelectOwnerRpc(false, NetworkManager.Singleton.LocalClientId);
            }

            if (IsOwner)
            {
                m_IsInteracting.Value = false;
                SetOwnershipLockSafe(false);
                RelinquishOwnershipAfterTime();
            }
        }

        [Rpc(SendTo.Server)]
        void ResetObjectToServerRpc()
        {
            if (NetworkObject.OwnerClientId != NetworkManager.ServerClientId)
                NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }

        [Rpc(SendTo.Owner)]
        void ResetObjectToSessionOwnerRpc()
        {
            if (NetworkObject.OwnerClientId != NetworkManager.Singleton.CurrentSessionOwner)
                NetworkObject.ChangeOwnership(NetworkManager.Singleton.CurrentSessionOwner);
        }

        [Rpc(SendTo.Server)]
        public virtual void OnSelectServerRpc(bool selected, ulong clientId)
        {
            OnSelectRpc(selected, clientId);

            if (selected && OwnerClientId != clientId)
            {
                NetworkObject.ChangeOwnership(clientId);
            }

            SelectNetworkedEventServer.Invoke(selected);
        }

        [Rpc(SendTo.Owner)]
        public virtual void OnSelectOwnerRpc(bool selected, ulong clientId)
        {
            OnSelectRpc(selected, clientId);

            if (selected && OwnerClientId != clientId)
            {
                SetOwnershipLockSafe(false);
                NetworkObject.ChangeOwnership(clientId);
            }

            SelectNetworkedEventServer.Invoke(selected);
        }

        [Rpc(SendTo.Everyone)]
        public virtual void OnSelectRpc(bool selected, ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
                Selected(selected);
        }

        public virtual void Selected(bool selected) { }

        public virtual void OnActivateLocal(BaseInteractionEventArgs args)
        {
            if (!IsSpawned || !IsOwner) return;

            Activated(true);
            if (syncActivate)
                OnActivateOwnerRpc(true, NetworkManager.Singleton.LocalClientId);
        }

        public virtual void OnDeactivateLocal(BaseInteractionEventArgs args)
        {
            if (!IsSpawned || !IsOwner) return;

            Activated(false);
            if (syncActivate)
                OnActivateOwnerRpc(false, NetworkManager.Singleton.LocalClientId);
        }

        [Rpc(SendTo.Owner)]
        public virtual void OnActivateOwnerRpc(bool activate, ulong clientId)
        {
            OnActivateRpc(activate, clientId);
            if (m_UseActivateEvents)
                ActivateNetworkedEventServer.Invoke(activate);
        }

        [Rpc(SendTo.Everyone)]
        public virtual void OnActivateRpc(bool activate, ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                Activated(activate);
#pragma warning disable CS0618
                m_AffordanceStateProvider?.UpdateAffordanceState(new AffordanceStateData(Convert.ToByte(activate ? 5 : isInteracting ? 4 : 0), 1.0f));
#pragma warning restore CS0618
            }
        }

        public virtual void Activated(bool activate)
        {
            if (m_UseActivateEvents)
                ActivateNetworkedEventAll.Invoke(activate);
        }

        protected virtual bool CanHold()
        {
            return !isInteracting || allowOverrideOwnership;
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();

            if (IsOwner && isInteracting && !baseInteractable.isSelected)
                m_IsInteracting.Value = false;

            if (IsOwner)
            {
                if (m_HostInteractionCheckEnumerator != null)
                    StopCoroutine(m_HostInteractionCheckEnumerator);

                m_HostInteractionCheckEnumerator = CheckForOwnerInteraction();
                StartCoroutine(m_HostInteractionCheckEnumerator);

                if (m_RelinquishToHostEnumerator != null)
                    StopCoroutine(m_RelinquishToHostEnumerator);

                if (baseInteractable.isSelected && !isInteracting)
                {
                    if (!IsSelectedBySocket())
                    {
                        m_IsInteracting.Value = true;
                    }
                }

                SetOwnershipLockSafe(m_IsInteracting.Value);
            }
        }

        public override void OnLostOwnership()
        {
            base.OnLostOwnership();

            if (!IsOwner)
            {
                if (m_HostInteractionCheckEnumerator != null)
                    StopCoroutine(m_HostInteractionCheckEnumerator);

                if (m_RelinquishToHostEnumerator != null)
                    StopCoroutine(m_RelinquishToHostEnumerator);

                if (baseInteractable.isSelected)
                    m_InteractionManager.CancelInteractableSelection((IXRSelectInteractable)baseInteractable);
            }
        }

        IEnumerator CheckForOwnerInteraction()
        {
            while (IsOwner)
            {
                if (isInteracting)
                {
                    if (!baseInteractable.isSelected || IsSelectedBySocket())
                    {
                        Utils.Log($"Interacting is true and either selected is false or selected is a socket on object {gameObject.name}. Is this intentional?");
                        m_IsInteracting.Value = false;
                    }
                }
                yield return new WaitForSeconds(interactionCheckTime);
            }
        }

        bool IsSelectedBySocket()
        {
            if (baseInteractable.isSelected)
            {
                foreach (var interactor in baseInteractable.interactorsSelecting)
                {
                    if (interactor is XRSocketInteractor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected virtual void OnIsInteractingChanged(bool oldValue, bool newValue)
        {
            OnInteractingChanged.Invoke(newValue);

#pragma warning disable CS0618
            m_AffordanceStateProvider?.UpdateAffordanceState(new AffordanceStateData(Convert.ToByte(newValue ? 4 : 0), 1.0f));
#pragma warning restore CS0618

            SelectNetworkedEventAll.Invoke(newValue);

            if (newValue && IsOwner && m_RelinquishToHostEnumerator != null)
                StopCoroutine(m_RelinquishToHostEnumerator);
        }

        protected void RelinquishOwnershipAfterTime()
        {
            if (!m_RelinquishOwnershipAfterTime) return;

            if (m_RelinquishToHostEnumerator != null) StopCoroutine(m_RelinquishToHostEnumerator);
            m_RelinquishToHostEnumerator = RelinquishOwnershipToHost();
            StartCoroutine(m_RelinquishToHostEnumerator);
        }

        IEnumerator RelinquishOwnershipToHost()
        {
            yield return new WaitForSeconds(relinquishOwnershipTime);
            if (!baseInteractable.isSelected)
            {
                if (NetworkManager.Singleton != null &&
                    NetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer)
                {
                    ResetObjectToServerRpc();
                }
                else if (!IsOwner)
                {
                    ResetObjectToSessionOwnerRpc();
                }
            }
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return IsOwner || allowOverrideOwnership || (!IsOwner & !isInteracting);
        }

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            return IsOwner || allowOverrideOwnership || (!IsOwner & !isInteracting);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkBaseInteractable), true), CanEditMultipleObjects]
    public class NetworkBaseInteractableEditor : Editor
    {
        SerializedProperty m_UseInteractingChangedEvent;
        SerializedProperty m_InteractingChangedEvent;
        SerializedProperty m_SyncHover;
        SerializedProperty m_UseHoverEvents;
        SerializedProperty m_SyncHoverEventServer;
        SerializedProperty m_SyncHoverEventAll;
        SerializedProperty m_SyncSelect;
        SerializedProperty m_UseSelectEvents;
        SerializedProperty m_SyncSelectEventServer;
        SerializedProperty m_SyncSelectEventAll;
        SerializedProperty m_SyncActivate;
        SerializedProperty m_UseActivateEvents;
        SerializedProperty m_SyncActivateEventServer;
        SerializedProperty m_SyncActivateEventAll;
        SerializedProperty m_FoldoutStates;

        void OnEnable()
        {
            m_UseInteractingChangedEvent = serializedObject.FindProperty("m_UseInteractingChangedEvent");
            m_InteractingChangedEvent = serializedObject.FindProperty("OnInteractingChanged");
            m_SyncHover = serializedObject.FindProperty("m_SyncHover");
            m_UseHoverEvents = serializedObject.FindProperty("m_UseHoverEvents");
            m_SyncHoverEventServer = serializedObject.FindProperty("HoverNetworkedEventServer");
            m_SyncHoverEventAll = serializedObject.FindProperty("HoverNetworkedEventAll");
            m_SyncSelect = serializedObject.FindProperty("m_SyncSelect");
            m_UseSelectEvents = serializedObject.FindProperty("m_UseSelectEvents");
            m_SyncSelectEventServer = serializedObject.FindProperty("SelectNetworkedEventServer");
            m_SyncSelectEventAll = serializedObject.FindProperty("SelectNetworkedEventAll");
            m_SyncActivate = serializedObject.FindProperty("m_SyncActivate");
            m_UseActivateEvents = serializedObject.FindProperty("m_UseActivateEvents");
            m_SyncActivateEventServer = serializedObject.FindProperty("ActivateNetworkedEventServer");
            m_SyncActivateEventAll = serializedObject.FindProperty("ActivateNetworkedEventAll");
            m_FoldoutStates = serializedObject.FindProperty("m_FoldoutValues");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Unity Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_UseInteractingChangedEvent);
            if (m_UseInteractingChangedEvent.boolValue)
            {
                EditorGUILayout.PropertyField(m_InteractingChangedEvent);
            }

            EditorGUILayout.Space(10);
            SerializedProperty option = m_FoldoutStates.GetArrayElementAtIndex(0);
            option.boolValue = EditorGUILayout.Foldout(option.boolValue, "Hover Options", true);
            if (option.boolValue)
            {
                if (m_UseHoverEvents.boolValue)
                {
                    m_SyncHover.boolValue = true;
                    GUI.enabled = false;
                }

                EditorGUILayout.PropertyField(m_SyncHover);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(m_UseHoverEvents);

                if (m_UseHoverEvents.boolValue)
                {
                    EditorGUILayout.PropertyField(m_SyncHoverEventServer);
                    EditorGUILayout.PropertyField(m_SyncHoverEventAll);
                }
            }

            EditorGUILayout.Space(10);
            option = m_FoldoutStates.GetArrayElementAtIndex(1);
            option.boolValue = EditorGUILayout.Foldout(option.boolValue, "Select Options", true);
            if (option.boolValue)
            {
                if (m_UseSelectEvents.boolValue)
                {
                    m_SyncSelect.boolValue = true;
                    GUI.enabled = false;
                }

                EditorGUILayout.PropertyField(m_SyncSelect);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(m_UseSelectEvents);

                if (m_UseSelectEvents.boolValue)
                {
                    EditorGUILayout.PropertyField(m_SyncSelectEventServer);
                    EditorGUILayout.PropertyField(m_SyncSelectEventAll);
                }
            }

            EditorGUILayout.Space(10);
            option = m_FoldoutStates.GetArrayElementAtIndex(2);
            option.boolValue = EditorGUILayout.Foldout(option.boolValue, "Activate Options", true);
            if (option.boolValue)
            {
                if (m_UseActivateEvents.boolValue)
                {
                    m_SyncActivate.boolValue = true;
                    GUI.enabled = false;
                }

                EditorGUILayout.PropertyField(m_SyncActivate);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(m_UseActivateEvents);

                if (m_UseActivateEvents.boolValue)
                {
                    EditorGUILayout.PropertyField(m_SyncActivateEventServer);
                    EditorGUILayout.PropertyField(m_SyncActivateEventAll);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
