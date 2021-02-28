﻿using System.Collections.Generic;
using System.Linq;
using Configs.Game;
using Configs.Level;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace App.Level
{
    public interface ILevelManager
    {
        void GenerateLevel(GameModePvE applicationConfigGameModePvE, LevelConfig levelConfig);
    }

    public class LevelManager : ILevelManager
    {
        public LevelState CurrentLevelState { get; }

        public void GenerateLevel(GameModePvE gameModePvE, LevelConfig levelConfig)
        {
            Assert.IsTrue(ApplicationHolder.Instance.TryGet<ISceneManager>(out var sceneManager));

            var levelGridModel = new LevelGridModel(levelConfig.LevelStages.First());

            var levelStageConfig = levelConfig.LevelStages.First();
            SetupCamera(levelConfig, levelStageConfig, levelGridModel);

            InstantiateGameObjects(levelConfig, levelGridModel);

            SetupWalls(levelConfig, levelGridModel);

            foreach (var spawnCorner in levelStageConfig.PlayersSpawnCorners)
            {
                var position = (math.mad(spawnCorner, 2, - 1) * (float2) (levelGridModel.Size - 1) / 2.0f).xyy * math.float3(1, 1, 0);
                Object.Instantiate(gameModePvE.BombermanConfig.Prefab, position, Quaternion.identity);
            }

            /*PrefabsManager.Instantiate();

            EnemiesManager.PopulateLevel(levelConfig, levelMode);
            PlayersManager.PopulateLevel(levelConfig, levelMode);*/
        }

        private static void SetupWalls(LevelConfig levelConfig, LevelGridModel levelGridModel)
        {
            var columnsNumber = levelGridModel.ColumnsNumber;
            var rowsNumber = levelGridModel.RowsNumber;

            var walls = Object.Instantiate(levelConfig.Walls, Vector3.zero, Quaternion.identity);
            var sprite = walls.GetComponent<SpriteRenderer>();
            sprite.size += new Vector2(columnsNumber, rowsNumber);

            var offsetsAndSize = new[]
            {
                (math.float2(+columnsNumber / 2.0f + 1, 0), math.float2(2, rowsNumber)),
                (math.float2(-columnsNumber / 2.0f - 1, 0), math.float2(2, rowsNumber)),
                (math.float2(0, +rowsNumber / 2.0f + 1), math.float2(columnsNumber, 2)),
                (math.float2(0, -rowsNumber / 2.0f - 1), math.float2(columnsNumber, 2))
            };

            foreach (var (offset, size) in offsetsAndSize)
            {
                var collider = walls.AddComponent<BoxCollider2D>();
                collider.offset = offset;
                collider.size = size;
            }
        }

        private static void InstantiateGameObjects(LevelConfig levelConfig, LevelGridModel levelGridModel)
        {
            var columnsNumber = levelGridModel.ColumnsNumber;
            var rowsNumber = levelGridModel.RowsNumber;

            var hardBlocksGroup = new GameObject("HardBlocks");
            var softBlocksGroup = new GameObject("SoftBlocks");

            // :TODO: refactor
            var blocks = new Dictionary<GridTileType, (GameObject, GameObject)>
            {
                {GridTileType.HardBlock, (hardBlocksGroup, levelConfig.HardBlock.Prefab)},
                {GridTileType.SoftBlock, (softBlocksGroup, levelConfig.SoftBlock.Prefab)}
            };

            var startPosition = (math.float3(1) - math.float3(columnsNumber, rowsNumber, 0)) / 2;

            for (var index = 0; index < columnsNumber * rowsNumber; ++index)
            {
                var blockType = levelGridModel[index];

                if (blockType == GridTileType.FloorTile)
                    continue;

                // ReSharper disable once PossibleLossOfFraction
                var position = startPosition + math.float3(index % columnsNumber, index / columnsNumber, 0);

                var (parent, prefab) = blocks[blockType];
                Object.Instantiate(prefab, position, Quaternion.identity, parent.transform);
            }
        }

        private static void SetupCamera(LevelConfig levelConfig, LevelStageConfig levelStageConfig,
            LevelGridModel levelGridModel)
        {
            var mainCamera = Camera.main;
            if (!mainCamera)
                return;

            var cameraRect = math.float2(Screen.width * 2.0f / Screen.height, 1) * mainCamera.orthographicSize;

            var fieldRect = (levelGridModel.Size - cameraRect) / 2.0f;
            var fieldMargins = (float4) levelConfig.ViewportPadding / levelConfig.OriginalPixelsPerUnits;

            var firstPlayerCorner = levelStageConfig.PlayersSpawnCorners.FirstOrDefault();

            var camePosition = (firstPlayerCorner - (float2) 0.5f) * levelGridModel.Size;
            camePosition = math.clamp(camePosition, fieldMargins.xy - fieldRect, fieldRect + fieldMargins.zw);

            mainCamera.transform.position = math.float3(camePosition, -1);
        }
    }
}
