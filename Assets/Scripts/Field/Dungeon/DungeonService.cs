using System;
using System.Globalization;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Mage
{
    /// <summary>고블린 던전 캡슐 (MonoBehaviour 아님. FieldState가 만들고 이음): 던전 판 데이터·씬 참조·현장 규칙을 받아, 던전이 없거나 새로 만들 차례면 세대를 올리고 보스를 고른 뒤 적 자리마다 적(쓰러진 키는 시체)·보스를 놓음. 보스가 쓰러지면 Cleared로 알림. 몬스터가 보관한 금화를 모아 두고 챙기면 내줌. 숲을 나갈 때 클리어했으면 다음엔 새 던전</summary>
    public sealed partial class DungeonService
    {
        /// <summary>던전 무리 번호 시작 (숲 무리와 안 겹치게)</summary>
        const int PackBase = 1000;

        /// <summary>부하를 보스 둘레에 흩는 반지름 (m)</summary>
        const float SummonSpread = 6f;

        /// <summary>던전 적 키 형식 ({0} 던전 세대, {1} 자리 id, {2} 번호)</summary>
        const string EnemyKeyFormat = "dungeon.{0}.{1}.{2}";

        /// <summary>보스 키 형식 ({0} 던전 세대)</summary>
        const string BossKeyFormat = "dungeon.{0}.boss";

        /// <summary>던전 판 데이터</summary>
        readonly DungeonEntry _data;

        /// <summary>던전 씬 참조</summary>
        readonly DungeonView _view;

        /// <summary>현장 규칙 (놓은 적·부하가 씀)</summary>
        readonly FieldService _field;

        /// <summary>보스를 쓰러뜨려 던전을 클리어함</summary>
        public event Action Cleared;

        #region DungeonService - 던전 판 데이터·씬 참조·현장 규칙을 받음
        /// <param name="data">던전 판 데이터</param>
        /// <param name="view">던전 씬 참조</param>
        /// <param name="field">현장 규칙</param>
        public DungeonService(DungeonEntry data, DungeonView view, FieldService field)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }
        #endregion

        #region Spawn - 던전을 준비하고 적 자리마다 수만큼 놓음 (쓰러진 키는 시체, 키에 던전 세대가 들어감), 보스를 놓음. 무리 번호는 숲과 안 겹치게
        public void Spawn()
        {
            Prepare();
            // 무리 번호 (던전 자리마다 하나)
            int pack = PackBase;
            foreach (SpawnPointView point in _view.SpawnPoints)
            {
                if (point == null || point.Monster == null)
                {
                    Debug.LogWarning("[Dungeon] 비었거나 몬스터가 없는 던전 적 자리", point);
                    continue;
                }
                pack++;
                for (int i = 0; i < point.Count; i++)
                {
                    // 이 적 키
                    string key = string.Format(CultureInfo.InvariantCulture, EnemyKeyFormat, _data.Generation, point.Id, i);
                    // 쓰러졌는지, 쓰러진 곳
                    bool dead = _data.Dead.TryGetValue(key, out Vector3 corpseAt);
                    // 놓은 적
                    EnemyController enemy = Object.Instantiate(_view.EnemyPrefab, FieldService.Sample(dead ? corpseAt : SpawnPosition(point)), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    if (dead)
                    {
                        enemy.InitCorpse(point.Monster, key, pack);
                        continue;
                    }
                    enemy.Init(point.Monster, _field, key, pack);
                    enemy.Died += OnEnemyDied;
                }
            }
            SpawnBoss(pack + 1);
        }
        #endregion

        #region AddStash - 몬스터가 던전에 보관한 금화를 더함 (0 이하면 무시)
        /// <param name="gold">금화</param>
        public void AddStash(int gold)
        {
            if (gold > 0) _data.StashGold += gold;
        }
        #endregion

        #region TakeStash - 보관 금화를 다 내줌 (없으면 0)
        public int TakeStash()
        {
            // 내줄 금화
            int gold = _data.StashGold;
            _data.StashGold = 0;
            return gold;
        }
        #endregion

        #region MarkLeaving - 숲을 나갈 때: 클리어했으면 다음 진입에 새 던전, 보관 금화는 사라짐
        public void MarkLeaving()
        {
            if (!_data.Cleared) return;
            _data.NeedsReset = true;
            _data.StashGold = 0;
        }
        #endregion

        #region Prepare - 던전이 아직 없거나 새로 만들 차례면: 세대를 올리고 쓰러진 기록·클리어·보관 금화를 비우고 보스를 후보 중 무작위로 고름 (모양은 고정, 무작위 형태는 보류)
        void Prepare()
        {
            if (_data.BossId != null && !_data.NeedsReset) return;
            _data.Generation++;
            _data.Dead.Clear();
            _data.Cleared = false;
            _data.NeedsReset = false;
            _data.StashGold = 0;
            // 보스 후보
            MonsterSO[] choices = _view.BossChoices;
            // 고른 보스
            MonsterSO pick = choices != null && choices.Length > 0 ? choices[Random.Range(0, choices.Length)] : null;
            _data.BossId = pick != null ? pick.Id : string.Empty;
        }
        #endregion

        #region SpawnBoss - 이번 던전 보스를 보스 자리에 놓음 (쓰러졌으면 시체). 패턴에 현장 규칙·부하 부르기를 넘김
        /// <param name="pack">보스 무리 번호 (부하도 같음)</param>
        void SpawnBoss(int pack)
        {
            // 보스 데이터
            MonsterSO boss = FindBoss(_data.BossId);
            if (boss == null)
            {
                Debug.LogWarning($"[Dungeon] 던전 보스 없음: {_data.BossId}");
                return;
            }
            // 보스 키
            string key = string.Format(CultureInfo.InvariantCulture, BossKeyFormat, _data.Generation);
            // 쓰러졌는지, 쓰러진 곳
            bool dead = _data.Dead.TryGetValue(key, out Vector3 corpseAt);
            // 놓은 보스
            EnemyController enemy = Object.Instantiate(_view.BossPrefab, FieldService.Sample(dead ? corpseAt : _view.BossPoint.position), _view.BossPoint.rotation);
            if (dead)
            {
                enemy.InitCorpse(boss, key, pack);
                return;
            }
            enemy.Init(boss, _field, key, pack);
            enemy.Died += OnBossDied;
            if (enemy.TryGetComponent(out BossPatternController pattern))
                pattern.Init(_field, (monster, near) => Summon(monster, near, pack));
        }
        #endregion

        #region FindBoss - 보스 후보에서 id로 찾기 (대소문자 구분). 없으면 null
        /// <param name="id">몬스터 id</param>
        MonsterSO FindBoss(string id)
        {
            if (string.IsNullOrEmpty(id) || _view.BossChoices == null) return null;
            foreach (MonsterSO monster in _view.BossChoices)
                if (monster != null && string.Equals(monster.Id, id, StringComparison.Ordinal)) return monster;
            return null;
        }
        #endregion

        #region Summon - 보스 둘레에 부하 하나를 놓고 마법사를 쫓게 함 (키가 없어 판 데이터에 안 남음)
        /// <param name="monster">부하 데이터</param>
        /// <param name="near">보스 자리</param>
        /// <param name="pack">보스 무리 번호</param>
        EnemyController Summon(MonsterSO monster, Vector3 near, int pack)
        {
            // 흩는 거리
            Vector2 offset = Random.insideUnitCircle * SummonSpread;
            // 놓은 부하
            EnemyController enemy = Object.Instantiate(_view.EnemyPrefab, FieldService.Sample(near + new Vector3(offset.x, 0f, offset.y)), Quaternion.identity);
            enemy.Init(monster, _field, string.Empty, pack);
            enemy.Wake(_field.Player);
            return enemy;
        }
        #endregion

        #region OnEnemyDied - 던전 적이 쓰러진 곳을 판 데이터에 남김
        /// <param name="enemy">쓰러진 적</param>
        void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;
            _data.Dead[enemy.SpawnKey] = enemy.transform.position;
        }
        #endregion

        #region OnBossDied - 보스가 쓰러짐: 판 데이터에 남기고 클리어, 알림
        /// <param name="enemy">쓰러진 보스</param>
        void OnBossDied(EnemyController enemy)
        {
            enemy.Died -= OnBossDied;
            _data.Dead[enemy.SpawnKey] = enemy.transform.position;
            _data.Cleared = true;
            Cleared?.Invoke();
        }
        #endregion

        #region SpawnPosition - 적 자리 둘레 흩어 놓는 반지름 안 아무 곳
        /// <param name="point">적 자리</param>
        static Vector3 SpawnPosition(SpawnPointView point)
        {
            // 흩어 놓는 거리
            Vector2 offset = Random.insideUnitCircle * point.Spread;
            return point.transform.position + new Vector3(offset.x, 0f, offset.y);
        }
        #endregion
    }
}
