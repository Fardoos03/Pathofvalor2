using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataController : MonoBehaviour {

    public static DataController instance = null;
    public GameData data;

    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);

        }
        else {
            Destroy(gameObject);
            return;
        }
        EnsureDataInitialized();
        RefreshData();
    }

    private void EnsureDataInitialized() {
        if (data == null) {
            data = new GameData();
        }
    }

    public static DataController EnsureInstance() {
        if (instance != null) {
            instance.EnsureDataInitialized();
            return instance;
        }

        DataController existing = FindFirstObjectByType<DataController>();
        if (existing != null) {
            instance = existing;
            instance.EnsureDataInitialized();
            return instance;
        }

        GameObject controllerObject = new GameObject(nameof(DataController));
        DataController controller = controllerObject.AddComponent<DataController>();
        controller.EnsureDataInitialized();
        controller.RefreshData();
        return controller;
    }

    public void RefreshData() {
        EnsureDataInitialized();
        string key = Application.isEditor ? "MySettingsEditor" : "MySettings";
        string jsonData = PlayerPrefs.GetString(key, string.Empty);
        if (!string.IsNullOrEmpty(jsonData)) {
            JsonUtility.FromJsonOverwrite(jsonData, data);
        }
    }

    public void SaveData(bool isResetClicked = false) {
        EnsureDataInitialized();
        //Convert to Json
        string jsonData = JsonUtility.ToJson(data);
        if (isResetClicked)
            jsonData = "";
        if (Application.isEditor) {
            //Save Json string
            PlayerPrefs.SetString("MySettingsEditor", jsonData);
            PlayerPrefs.Save();
        }
        else {
            //Save Json string
            PlayerPrefs.SetString("MySettings", jsonData);
            PlayerPrefs.Save();
        }
    }

    public void SaveData(GameData saveData) {
        EnsureDataInitialized();
        //Convert to Json
        string jsonData = JsonUtility.ToJson(saveData);
        if (Application.isEditor) {
            //Save Json string
            PlayerPrefs.SetString("MySettingsEditor", jsonData);
            PlayerPrefs.Save();
        }
        else {
            //Save Json string
            PlayerPrefs.SetString("MySettings", jsonData);
            PlayerPrefs.Save();
        }
    }

    private void OnApplicationQuit() {
        //SaveData();
    }

    

}
