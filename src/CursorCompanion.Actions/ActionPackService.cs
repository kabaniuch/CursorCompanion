using System.Text.Json;
using CursorCompanion.Core;

namespace CursorCompanion.Actions;

public class ActionPackService
{
    private readonly Dictionary<string, ActionDefinition> _actions = new();
    private readonly List<ActionPack> _packs = new();
    private ActionPack? _activePack;
    private float _globalCooldown = 3f;
    private float _cooldownTimer;

    public IReadOnlyList<ActionPack> Packs => _packs;
    public ActionPack? ActivePack => _activePack;
    public bool IsOnCooldown => _cooldownTimer > 0;

    public void Load(string actionsJsonPath, string packsJsonPath)
    {
        try
        {
            if (File.Exists(actionsJsonPath))
            {
                var json = File.ReadAllText(actionsJsonPath);
                var data = JsonSerializer.Deserialize<ActionsData>(json);
                if (data?.Actions != null)
                {
                    foreach (var action in data.Actions)
                        _actions[action.Id] = action;
                }
            }

            if (File.Exists(packsJsonPath))
            {
                var json = File.ReadAllText(packsJsonPath);
                var data = JsonSerializer.Deserialize<PacksData>(json);
                if (data?.Packs != null)
                    _packs.AddRange(data.Packs);
            }

            if (_packs.Count > 0)
                _activePack = _packs[0];

            Logger.Info($"Loaded {_actions.Count} actions, {_packs.Count} packs");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load actions", ex);
            LoadDefaults();
        }

        if (_actions.Count == 0)
            LoadDefaults();
    }

    private void LoadDefaults()
    {
        var defaults = new[]
        {
            new ActionDefinition { Id = "scratch", Name = "Scratch", ClipName = "Scratch" },
            new ActionDefinition { Id = "roar", Name = "Roar", ClipName = "Roar" },
            new ActionDefinition { Id = "pawwave", Name = "Paw Wave", ClipName = "PawWave" },
            new ActionDefinition { Id = "shake", Name = "Shake", ClipName = "Shake" },
            new ActionDefinition { Id = "sitpose", Name = "Sit Pose", ClipName = "SitPose" },
        };

        foreach (var a in defaults)
            _actions[a.Id] = a;

        _packs.Add(new ActionPack
        {
            Id = "default",
            Name = "Default",
            ActionIds = defaults.Select(a => a.Id).ToList()
        });

        _activePack = _packs[0];
    }

    public void SetActivePack(string packId)
    {
        _activePack = _packs.FirstOrDefault(p => p.Id == packId);
    }

    public void SetGlobalCooldown(float seconds)
    {
        _globalCooldown = seconds;
    }

    public void Update(float dt)
    {
        if (_cooldownTimer > 0)
            _cooldownTimer -= dt;
    }

    public ActionDefinition? TriggerAction(int index)
    {
        if (_activePack == null || index < 0 || index >= _activePack.ActionIds.Count)
            return null;

        if (_cooldownTimer > 0)
            return null;

        var actionId = _activePack.ActionIds[index];
        if (!_actions.TryGetValue(actionId, out var action))
            return null;

        _cooldownTimer = action.CooldownOverride ?? _globalCooldown;
        Logger.Info($"Action triggered: {action.Name} (cooldown: {_cooldownTimer}s)");
        return action;
    }

    public ActionDefinition? GetAction(string actionId)
    {
        _actions.TryGetValue(actionId, out var action);
        return action;
    }
}
