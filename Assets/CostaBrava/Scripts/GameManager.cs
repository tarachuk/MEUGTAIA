using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace CostaBrava
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public PlayerController player;
        public ThirdPersonCamera followCamera;
        public WantedSystem wanted;
        public MissionSystem missions;
        public MoneySystem money;
        public ShopSystem shop;
        public WeaponShop weaponShop;
        public GameHUD hud;
        public AudioManager audioManager;
        public Vector3 spawnPoint = new Vector3(-8, .5f, -32);
        public bool Paused { get; private set; }
        public bool Started { get; private set; }
        public bool MenuOpen => shop.IsOpen || (weaponShop && weaponShop.IsOpen);
        public bool Blocked => !Started || Paused || MenuOpen || player.health.Dead;
        void Awake() { Instance = this; Time.timeScale = 1; Application.targetFrameRate = 60; }
        void Start() { Started = false; SetCursor(false); }
        void Update()
        {
            if (!Started && GameInput.Pressed(Key.Enter)) Begin();
            if (GameInput.Pressed(Key.Escape) && Started && !player.health.Dead)
            {
                if (shop.IsOpen) shop.Close();else if(weaponShop && weaponShop.IsOpen)weaponShop.Close();else Pause(!Paused);
            }
            if (Started && !Paused && !MenuOpen && !player.health.Dead && Cursor.lockState != CursorLockMode.Locked && GameInput.FirePressed) SetCursor(true);
        }
        public void Begin() { Started=true; SetCursor(true); hud.Notify("Bem-vindo a Costa Brava", "Encontre os marcadores dourados para iniciar as missões."); }
        public void Pause(bool value) { Paused=value; Time.timeScale=value?0:1; SetCursor(!value); }
        public static void SetCursor(bool locked) { Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None; Cursor.visible=!locked; }
        public void Restart() { Time.timeScale=1; SetCursor(false); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public void Quit()
        {
            Time.timeScale=1; SetCursor(false);
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
        void OnDestroy() { Time.timeScale=1; SetCursor(false); if (Instance==this) Instance=null; }
    }
}
