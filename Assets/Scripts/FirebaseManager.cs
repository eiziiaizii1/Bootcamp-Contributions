using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement;
using StarterAssets;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    public GameManager gameManager;

    public GameObject usernamePanel, userProfilePanel, leaderBoardPanel, leaderBoardContent, userDataPrefab;
    public TMP_Text profileUsernameTxt, profileUserScoreTxt, errorUsernameTxt;
    public TMP_InputField usernameinput;
    public int totalUsers = 0;
    public string username = "";

    private DatabaseReference db;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        //// PlayerPrefs'i temizle
        //PlayerPrefs.DeleteAll();
        //PlayerPrefs.Save();

        FirebaseInitialize();

        // Sahne y�klendi�inde kullan�c� ad� ve skoru �ekin
        if (PlayerPrefs.HasKey("PlayerID"))
        {
            int playerID = PlayerPrefs.GetInt("PlayerID");
            StartCoroutine(FetchUserProfileData(playerID));
        }
    }

    void FirebaseInitialize()
    {
        db = FirebaseDatabase.DefaultInstance.GetReference("/Leaderboard1/");
        db.ChildAdded += HandleChildAdded;
        GetTotalUsers();
        int playerID = PlayerPrefs.GetInt("PlayerID", -1);
        if (playerID != -1)
        {
            StartCoroutine(FetchUserProfileData(playerID));
        }
        //StartCoroutine(FetchUserProfileData(PlayerPrefs.GetInt("PlayerID")));
    }

    public DatabaseReference GetDatabaseReference()
    {
        return db;
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            return;
        }
        GetTotalUsers();
    }

    void GetTotalUsers()
    {
        db.ValueChanged += (object sender2, ValueChangedEventArgs e2) =>
        {
            if (e2.DatabaseError != null)
            {
                Debug.LogError(e2.DatabaseError.Message);
                return;
            }
            totalUsers = int.Parse(e2.Snapshot.ChildrenCount.ToString());
            //Debug.LogError("Total users: " + totalUsers);
        };
    }

    public void ShowLeaderbord()
    {
        StartCoroutine(FetchLeaderBoardData());
    }

    public void SignInWithUsername(System.Action onSuccess)
    {
        StartCoroutine(CheckUserExistInDatabase(onSuccess));
    }

    public void SignInWithUsernameButton()
    {
        SignInWithUsername(OnSignInSuccess);
    }
    void OnSignInSuccess()
    {
        // Ba�ar�l� giri� durumunda yap�lacak i�lemler
        Debug.Log("Ba�ar�l� giri� yap�ld�");
    }

    public void CloseLeaderboard()
    {
        if (leaderBoardContent.transform.childCount > 0)
        {
            for (int i = 0; i < leaderBoardContent.transform.childCount; i++)
            {
                Destroy(leaderBoardContent.transform.GetChild(i).gameObject);
            }
        }
        leaderBoardPanel.SetActive(false);
        userProfilePanel.SetActive(true);
    }

    public void SignOut()
    {
        PlayerPrefs.DeleteKey("PlayerID");
        PlayerPrefs.DeleteKey("Username");
        usernameinput.text = "";
        profileUsernameTxt.text = "";
        profileUserScoreTxt.text = "";
        usernamePanel.SetActive(true);
        userProfilePanel.SetActive(false);
    }

    IEnumerator CheckUserExistInDatabase(System.Action onSuccess)
    {
        var task = db.OrderByChild("Username").EqualTo(usernameinput.text).GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("Invalid Error");
            errorUsernameTxt.text = "Invalid error";
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot != null && snapshot.HasChildren)
            {
                //Debug.LogError("Username Exist");
                //errorUsernameTxt.text = "Username already exist";
                foreach (DataSnapshot childSnapshot in snapshot.Children)
                {
                    int playerID = int.Parse(childSnapshot.Key.Replace("User_", ""));
                    PlayerPrefs.SetInt("PlayerID", playerID);
                    PlayerPrefs.SetString("Username", usernameinput.text);
                    PlayerPrefs.Save();
                    Debug.Log("PlayerID and Username saved to PlayerPrefs: " + playerID + ", " + usernameinput.text);
                    StartCoroutine(FetchUserProfileData(playerID));

                    onSuccess?.Invoke(); // Giri� ba�ar�l�ysa callback �a�r�l�r

                    // Start game and switch panels
                    StartGame();
                    OnNextLevelButtonClicked();
                    yield break;
                }
            }
            else
            {
                Debug.LogError("Username Not Exist");
                PushUserData();
                PlayerPrefs.SetInt("PlayerID", totalUsers + 1);
                PlayerPrefs.SetString("Username", usernameinput.text);
                PlayerPrefs.Save();
                Debug.Log("PlayerID and Username saved to PlayerPrefs: " + (totalUsers + 1) + ", " + usernameinput.text);
                //StartCoroutine(delayFetchProfile());
                StartCoroutine(FetchUserProfileData(totalUsers + 1));

                onSuccess?.Invoke(); // Giri� ba�ar�l�ysa callback �a�r�l�r

                // Start game and switch panels
                StartGame();
                OnNextLevelButtonClicked();
            }
        }
    }

    void StartGame()
    {
        usernamePanel.SetActive(false);
        userProfilePanel.SetActive(false);
        leaderBoardPanel.SetActive(false);


        if (gameManager != null)
        {
            gameManager.StartGame();
        }
        else
        {
            Debug.LogError("GameManager instance is not assigned.");
        }
    }

    public void OnNextLevelButtonClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }


    IEnumerator delayFetchProfile()
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(FetchUserProfileData(totalUsers));
    }

    void PushUserData()
    {
        db.Child("User_" + (totalUsers + 1).ToString()).Child("Username").SetValueAsync(usernameinput.text);
        db.Child("User_" + (totalUsers + 1).ToString()).Child("score").SetValueAsync(0);
    }

    IEnumerator FetchUserProfileData(int playerID)
    {
        if (playerID != 0)
        {
            var task = db.Child("User_" + playerID.ToString()).GetValueAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Debug.LogError("Invalid Error");
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot != null && snapshot.HasChildren)
                {
                    username = snapshot.Child("Username").Value.ToString();
                    int score = int.Parse(snapshot.Child("score").Value.ToString());
                    profileUsernameTxt.text = username;
                    profileUserScoreTxt.text = score.ToString();
                    userProfilePanel.SetActive(true);
                    usernamePanel.SetActive(false);
                }
                else
                {
                    Debug.LogError("User ID not exist");
                }
            }
        }
    }

    IEnumerator FetchLeaderBoardData()
    {
        var task = db.OrderByChild("score").LimitToLast(10).GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("Invalid Error");
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            List<LeaderboardData1> listLeaderboardEntry = new List<LeaderboardData1>();

            foreach (DataSnapshot childSnapShot in snapshot.Children)
            {
                string username2 = childSnapShot.Child("Username").Value.ToString();
                int score = int.Parse(childSnapShot.Child("score").Value.ToString());
                listLeaderboardEntry.Add(new LeaderboardData1(username2, score));
            }
            DisplayLeaderBoardData(listLeaderboardEntry);
        }
    }

    void DisplayLeaderBoardData(List<LeaderboardData1> leaderboardData)
    {
        int rankCount = 0;
        for (int i = leaderboardData.Count - 1; i >= 0; i--)
        {
            rankCount++;
            GameObject obj = Instantiate(userDataPrefab);
            obj.transform.SetParent(leaderBoardContent.transform);
            obj.transform.localScale = Vector3.one;
            obj.GetComponent<UserDataUI>().userRankTxt.text = "Rank " + rankCount;
            obj.GetComponent<UserDataUI>().usernameTxt.text = leaderboardData[i].username;
            obj.GetComponent<UserDataUI>().userScoreTxt.text = leaderboardData[i].score.ToString();
        }
        leaderBoardPanel.SetActive(true);
        userProfilePanel.SetActive(false);
    }

    public void UpdateScore(int newScore)
    {
        StartCoroutine(UpdateUserScore(newScore));
    }

    public IEnumerator UpdateUserScore(int newScore)
    {
        int playerID = PlayerPrefs.GetInt("PlayerID");
        if (playerID != -1)
        {
            var task = db.Child("User_" + playerID.ToString()).Child("score").SetValueAsync(newScore);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Debug.LogError("Error updating score");
            }
            else
            {
                Debug.Log("Score updated successfully");
                profileUserScoreTxt.text = newScore.ToString();
            }
        }
    }
}

public class LeaderboardData1
{
    public string username;
    public int score;

    public LeaderboardData1(string username, int score)
    {
        this.username = username;
        this.score = score;
    }
}
