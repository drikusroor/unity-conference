using Photon.Pun;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;

[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(Camera))]

public class PlayerNetworkMover : MonoBehaviourPunCallbacks, IPunObservable {

    WebCamTexture webcamTexture;

    [SerializeField]
    private Animator animator;
    [SerializeField]
    private GameObject cameraObject;
    [SerializeField]
    private GameObject gunObject;
    [SerializeField]
    private GameObject playerObject;
    [SerializeField]
    private GameObject cameraProjector;
    [SerializeField]
    private NameTag nameTag;

    private Vector3 position;
    private Quaternion rotation;
    private bool jump;
    private float smoothing = 10.0f;

    /// <summary>
    /// Move game objects to another layer.
    /// </summary>
    void MoveToLayer(GameObject gameObject, int layer) {
        gameObject.layer = layer;
        foreach(Transform child in gameObject.transform) {
            MoveToLayer(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake() {
        // FirstPersonController script require cameraObject to be active in its Start function.
        if (photonView.IsMine) {
            cameraObject.SetActive(true);
            webcamTexture = new WebCamTexture();
            webcamTexture.Play();
        }
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start() {
        if (photonView.IsMine) {
            GetComponent<FirstPersonController>().enabled = true;
            MoveToLayer(gunObject, LayerMask.NameToLayer("Hidden"));
            MoveToLayer(playerObject, LayerMask.NameToLayer("Hidden"));
            // Set other player's nametag target to this player's nametag transform.
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players) {
                player.GetComponentInChildren<NameTag>().target = nameTag.transform;
            }
        }
        else {
            position = transform.position;
            rotation = transform.rotation;
            // Set this player's nametag target to other players's target.
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players) {
                if (player != gameObject) {
                    nameTag.target = player.GetComponentInChildren<NameTag>().target;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update() {
        if (!photonView.IsMine) {
            transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * smoothing);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * smoothing);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Texture2D snap = new Texture2D(webcamTexture.width, webcamTexture.height);
            var pixels = webcamTexture.GetPixels();
            snap.SetPixels(pixels);
            snap.Apply();
            var image = snap.EncodeToJPG(10);

            photonView.RPC("ChangeWebcamTexture", RpcTarget.All, image, webcamTexture.width, webcamTexture.height);
        }

    }

    [PunRPC]
    public void ChangeWebcamTexture(byte[] image, int textureWidth, int textureHeight)
    {
        var rend = cameraProjector.GetComponent<Renderer>();

        // duplicate the original texture and assign to the material
        Debug.Log('1');
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Debug.Log('2');
        texture.LoadImage(image);
        Debug.Log('3');
        rend.material.mainTexture = texture;
        Debug.Log('4');
    }

    /// <summary>
    /// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
    /// </summary>
    void FixedUpdate() {
        if (photonView.IsMine) {
            animator.SetFloat("Horizontal", CrossPlatformInputManager.GetAxis("Horizontal"));
            animator.SetFloat("Vertical", CrossPlatformInputManager.GetAxis("Vertical"));
            if (CrossPlatformInputManager.GetButtonDown("Jump")) {
                animator.SetTrigger("IsJumping");
            }
            animator.SetBool("Running", Input.GetKey(KeyCode.LeftShift));
        }
    }

    /// <summary>
    /// Used to customize synchronization of variables in a script watched by a photon network view.
    /// </summary>
    /// <param name="stream">The network bit stream.</param>
    /// <param name="info">The network message information.</param>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        } else {
            position = (Vector3)stream.ReceiveNext();
            rotation = (Quaternion)stream.ReceiveNext();
        }
    }

}
