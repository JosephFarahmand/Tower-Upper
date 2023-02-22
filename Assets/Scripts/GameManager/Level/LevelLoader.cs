using Lindon.TowerUpper.Data;
using Lindon.TowerUpper.Initilizer;
using Lindon.TowerUpper.Manager.Enemies;
using Lindon.TowerUpper.Profile;
using Unity.VisualScripting;
using UnityEngine;

namespace Lindon.TowerUpper.GameController.Level
{
    public class LevelLoader : MonoBehaviour, IInitilizer
    {
        public static LevelLoader Instance { get; private set; }

        public void Init()
        {
            if (Instance == null)
            {
                Instance = this;

                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            GameStarter.OnStartGame += StartGame;
        }

        private void OnDisable()
        {
            GameStarter.OnStartGame += StartGame;
        }

        private void StartGame()
        {
            var profile = ProfileController.Instance.Profile;

            var info = GetChapter(profile.Level);

            var gameInfo = GameData.Instance.GetGameInfo(info.Item1, info.Item2);

            Load(gameInfo, profile);
        }

        private (int, int) GetChapter(int profileLevel)
        {
            var chapterLevel = (profileLevel % 10);
            var gameLevel = profileLevel - chapterLevel * 10;
            return (chapterLevel + 1, gameLevel);
        }

        private void Load(GameInfo gameInfo, Profile.Profile profile)
        {
            LoadCharacter(profile);
            LoadTower(gameInfo);
            LoadEnemyManager(gameInfo);
        }

        private void LoadCharacter(Profile.Profile profile)
        {
            var characterSkinId = profile.GetActiveItem(ItemCategory.Skin);
            var weaponId = profile.GetActiveItem(ItemCategory.Weapon);
            GameManager.Instance.LevelInfo.SpawnCharacter(characterSkinId, weaponId);
        }

        private void LoadTower(GameInfo gameInfo)
        {
            GameManager.Instance.Tower.Components.SpawnPointCreator.CreatePoints(gameInfo.SpawnPointCount);
        }

        private void LoadEnemyManager(GameInfo gameInfo)
        {
            EnemyCounter.TotalEnemy = gameInfo.MaxEnemyCount;

            foreach (var id in gameInfo.EnemiesId)
            {
                var prefab = GameData.Instance.GetEnemyModel(id);
                var enemyPrefab = prefab.GetOrAddComponent<Enemy>();
                GameManager.Instance.EnemyManager.Generator.AddPrefab(enemyPrefab);
            }

            if (gameInfo.HasBossFight)
            {
                var prefab = GameData.Instance.GetEnemyModel(gameInfo.BossEnemyId);
                var enemyPrefab = prefab.GetOrAddComponent<Enemy>();
                GameManager.Instance.EnemyManager.Generator.AddBossPrefab(enemyPrefab);
            }
        }
    }
}