using System.Collections.Generic;
using UnityEngine;

// Reuses bullets instead of creating and destroying them, which avoids stutter on phones.
// Use ProjectilePool.Get(...) where you used Instantiate, and ProjectilePool.Release(...) (or the bullet's
// own Despawn) where you used Destroy. Nothing to add to the scene: it creates a "Projectile Pool" object
// by itself to keep the unused bullets in, and it starts fresh whenever the scene is reloaded.
public static class ProjectilePool
{
    // Unused bullets, kept per prefab (the turret's bullet and the thrower's bullet are separate stacks).
    static readonly Dictionary<Projectile, Stack<Projectile>> free = new Dictionary<Projectile, Stack<Projectile>>();
    static Transform root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { free.Clear(); root = null; }

    // The holder object. If the scene was reloaded it is gone, and so are the bullets that were in it.
    static Transform Root()
    {
        if (root == null)
        {
            free.Clear();
            root = new GameObject("Projectile Pool").transform;
        }
        return root;
    }

    static Stack<Projectile> StackFor(Projectile prefab)
    {
        if (!free.TryGetValue(prefab, out Stack<Projectile> stack))
        {
            stack = new Stack<Projectile>();
            free[prefab] = stack;
        }
        return stack;
    }

    // Takes a bullet from the pool (or makes a new one if the pool is empty), placed and ready to Launch.
    public static Projectile Get(Projectile prefab, Vector3 position, Quaternion rotation)
    {
        Transform parent = Root();
        Stack<Projectile> stack = StackFor(prefab);

        Projectile p = null;
        while (p == null && stack.Count > 0) p = stack.Pop();   // skips any that no longer exist

        if (p == null)
        {
            p = Object.Instantiate(prefab, position, rotation, parent);
            p.PrefabKey = prefab;
        }
        else
        {
            p.transform.SetPositionAndRotation(position, rotation);
            p.gameObject.SetActive(true);
        }

        p.OnSpawned();
        return p;
    }

    // Puts a bullet back for reuse.
    public static void Release(Projectile p)
    {
        if (p == null || p.InPool) return;

        // Not made by the pool (e.g. placed in the scene by hand), or the scene is going away: just destroy it.
        if (p.PrefabKey == null || root == null)
        {
            Object.Destroy(p.gameObject);
            return;
        }

        p.OnDespawned();
        p.gameObject.SetActive(false);
        StackFor(p.PrefabKey).Push(p);
    }

    // Makes sure at least 'count' unused bullets are ready, so the first shots don't have to create any.
    public static void Prewarm(Projectile prefab, int count)
    {
        if (prefab == null) return;

        Transform parent = Root();
        Stack<Projectile> stack = StackFor(prefab);

        while (stack.Count < count)
        {
            Projectile p = Object.Instantiate(prefab, parent);
            p.PrefabKey = prefab;
            p.InPool = true;
            p.gameObject.SetActive(false);
            stack.Push(p);
        }
    }
}
