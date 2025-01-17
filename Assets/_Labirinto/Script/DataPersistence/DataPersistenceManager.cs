using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using Helper;
using Unity.VisualScripting;

namespace DataPersistence
{
    public class DataPersistenceManager : SingletonMonobehaviour<DataPersistenceManager>
    {   
        [Header("Debugging")]
        [SerializeField] private bool disableDataPersistence = false;
        [SerializeField] private bool overrideSelectedProfileId = false;
        [SerializeField] private string testSelectedProfileId = "";

        [Header("File Storage Config")]
        [SerializeField] private string fileName;
        [SerializeField] private bool useEncryption;

        //[Header("Auto Saving Configuration")]
        //[SerializeField] private float autoSaveTimeSeconds = 60f;

        private GameData gameData;
        private PlayerData playerData;
        private List<IDataPersistence> dataPersistenceObjects;
        private List<IPlayerDataPersistence> playerDataPersistenceObjects;
    
        private FileDataHandler dataHandler;

        private string selectedProfileId = "";

        private Coroutine autoSaveCoroutine;

        public static DataPersistenceManager instance { get; private set; }

        public override void Awake() 
        {
            // Call the base SingletonMonobehaviour's Awake method
            base.Awake();

            // Prevent duplicate instances of the singleton
            if (Instance != this) 
            {
                Debug.Log("Found more than one Data Persistence Manager in the scene. Destroying the newest one.");
                Destroy(this.gameObject);
                return;
            }

            if (disableDataPersistence) 
            {
                Debug.LogWarning("Data Persistence is currently disabled!");
            }

            this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, useEncryption);

            InitializeSelectedProfileId();
        }

        private void OnEnable() 
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable() 
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
        {   
            Debug.Log("OnSceneLoaded Called");
            this.dataPersistenceObjects = FindAllDataPersistenceObjects();
            LoadGame();
            this.playerDataPersistenceObjects = FindAllPlayerDataPersistenceObjects();
            LoadPlayer();

            // Save the game data before handling the new scene
            SavePlayer();
            SaveGame();

            // start up the auto saving coroutine
            if (autoSaveCoroutine != null) 
            {
                StopCoroutine(autoSaveCoroutine);
            }
            //autoSaveCoroutine = StartCoroutine(AutoSave());
        }

        public void ChangeSelectedProfileId(string newProfileId) 
        {
            // update the profile to use for saving and loading
            this.selectedProfileId = newProfileId;
            // load the game, which will use that profile, updating our game data accordingly
            LoadGame();
        }

        public void DeleteProfileData(string profileId) 
        {
            // delete the data for this profile id
            dataHandler.Delete(profileId);
            // initialize the selected profile id
            InitializeSelectedProfileId();
            // reload the game so that our data matches the newly selected profile id
            LoadGame();
        }

        private void InitializeSelectedProfileId() 
        {
            this.selectedProfileId = dataHandler.GetMostRecentlyUpdatedProfileId();
            if (overrideSelectedProfileId) 
            {
                this.selectedProfileId = testSelectedProfileId;
                Debug.LogWarning("Overrode selected profile id with test id: " + testSelectedProfileId);
            }
        }

        // create player data
        public void NewPlayerData()
        {
            this.playerData = new PlayerData();   
        }

        public void NewGame(string playerSavedName) 
        {
            this.gameData = new GameData();
            this.gameData.playerSavedName = playerSavedName;

            Debug.Log("New game data created with save name: " + playerSavedName);
        }

        // SAVING THE PLAYER DATA
        public void LoadPlayer()
        {
            // load any saved data from a file using the data handler
            this.playerData = dataHandler.LoadPlayerData();

            foreach (IPlayerDataPersistence playerDataPersistenceObj in playerDataPersistenceObjects) 
            {
                playerDataPersistenceObj.LoadPlayerData(playerData);
            }
        }

        // LOADING THE GAME DATA ON SAVE SLOT
        public void LoadGame()
        {
            // return right away if data persistence is disabled
            if (disableDataPersistence) 
            {
                return;
            }

            // load any saved data from a file using the data handler
            this.gameData = dataHandler.Load(selectedProfileId);
            
            // if no data can be loaded, don't continue
            if (this.gameData == null) 
            {
                Debug.Log("No game data was found on save slots. A New Game needs to be started before data can be loaded.");
                return;
            }

            // push the loaded data to all other scripts that need it
            foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) 
            {
                dataPersistenceObj.LoadData(gameData);
            }
        }

        // SAVING THE PLAYER DATA
        public void SavePlayer()
        {
            if (this.playerData == null) 
            {
                Debug.LogWarning("No player data was found. A New Game needs to be started before data can be saved.");
                return;
            }

            foreach (IPlayerDataPersistence playerDataPersistenceObj in playerDataPersistenceObjects) 
            {
                playerDataPersistenceObj.SavePlayerData(playerData);
            }

            dataHandler.SavePlayerData(playerData);
        }

        // SAVING GAME DATA ON SAVE SLOT
        public void SaveGame()
        {
            // return right away if data persistence is disabled
            if (disableDataPersistence) 
            {
                return;
            }

            // if we don't have any data to save, log a warning here
            if (this.gameData == null) 
            {
                Debug.LogWarning("No game data on save slots was found. A New Game needs to be started before data can be saved.");
                return;
            }

            // pass the data to other scripts so they can update it
            foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) 
            {
                dataPersistenceObj.SaveData(gameData);
            }

            // timestamp the data so we know when it was last saved
            gameData.lastUpdated = System.DateTime.Now.ToBinary();

            // save that data to a file using the data handler
            dataHandler.Save(gameData, selectedProfileId);
        }

        private void OnApplicationQuit() 
        {
            SavePlayer();
            SaveGame();
        }

        private List<IPlayerDataPersistence> FindAllPlayerDataPersistenceObjects()
        {
            IEnumerable<IPlayerDataPersistence> playerDataPersistenceObjects = FindObjectsOfType<MonoBehaviour>(true)
                .OfType<IPlayerDataPersistence>();

            return new List<IPlayerDataPersistence>(playerDataPersistenceObjects);
        }

        private List<IDataPersistence> FindAllDataPersistenceObjects() 
        {
            // FindObjectsofType takes in an optional boolean to include inactive gameobjects
            IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsOfType<MonoBehaviour>(true)
                .OfType<IDataPersistence>();

            return new List<IDataPersistence>(dataPersistenceObjects);
        }

        public bool HasGameData() 
        {
            return gameData != null;
        }

        public bool HasPlayerData()
        {
            return playerData != null;
        }

        public Dictionary<string, GameData> GetAllProfilesGameData() 
        {
            return dataHandler.LoadAllProfiles();
        }

        /* private IEnumerator AutoSave()
        {
            while (true) 
            {
                yield return new WaitForSeconds(autoSaveTimeSeconds);
                SavePlayer();
                SaveGame();
                Debug.Log("Auto Saved Game");
            }
        } */
    }   
}