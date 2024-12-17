using System;
using SharpDX;
using SharpDX.DirectInput;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Timer = System.Threading.Timer;

namespace BatRun
{
    public class DInputHandler : IDisposable
    {
        private DirectInput? directInput;
        private readonly ConcurrentDictionary<Guid, Joystick> joysticks = new();
        private readonly Logger logger;
        private bool disposed;
        private readonly Timer cleanupTimer;
        private readonly ObjectPool<JoystickState> statePool;
        private const int CLEANUP_INTERVAL = 30000; // 30 secondes

        public DInputHandler(Logger logger)
        {
            this.logger = logger;
            directInput = new DirectInput();
            statePool = new ObjectPool<JoystickState>(() => new JoystickState(), 10);
            
            // Initialiser le timer de nettoyage
            cleanupTimer = new Timer(_ => CleanupDisconnectedDevices(), null, CLEANUP_INTERVAL, CLEANUP_INTERVAL);
            
            InitializeJoysticks();
        }

        private void InitializeJoysticks()
        {
            try
            {
                if (directInput == null)
                {
                    logger.LogError("DirectInput not initialized");
                    return;
                }

                var devices = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
                foreach (var deviceInstance in devices)
                {
                    try
                    {
                        var joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                        joystick.Acquire();
                        joysticks.TryAdd(deviceInstance.InstanceGuid, joystick);
                        logger.LogInfo($"DirectInput joystick initialized: {deviceInstance.InstanceName}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error initializing joystick {deviceInstance.InstanceName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error enumerating DirectInput devices: {ex.Message}");
            }
        }

        private void CleanupDisconnectedDevices()
        {
            try
            {
                if (disposed) return;

                var disconnectedDevices = new List<Guid>();

                foreach (var kvp in joysticks)
                {
                    try
                    {
                        var joystick = kvp.Value;
                        if (!IsJoystickConnected(joystick))
                        {
                            disconnectedDevices.Add(kvp.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error checking joystick status: {ex.Message}");
                        disconnectedDevices.Add(kvp.Key);
                    }
                }

                foreach (var guid in disconnectedDevices)
                {
                    if (joysticks.TryRemove(guid, out var joystick))
                    {
                        try
                        {
                            joystick.Unacquire();
                            joystick.Dispose();
                            logger.LogInfo($"Cleaned up disconnected joystick: {guid}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error disposing joystick: {ex.Message}");
                        }
                    }
                }

                // Forcer le GC si beaucoup de dispositifs ont été nettoyés
                if (disconnectedDevices.Count > 3)
                {
                    GC.Collect(0, GCCollectionMode.Optimized);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in cleanup routine: {ex.Message}");
            }
        }

        private bool IsJoystickConnected(Joystick joystick)
        {
            try
            {
                var state = statePool.Get();
                try
                {
                    joystick.GetCurrentState(ref state);
                    return true;
                }
                finally
                {
                    statePool.Return(state);
                }
            }
            catch
            {
                return false;
            }
        }

        public JoystickState GetJoystickState(Guid deviceGuid)
        {
            if (joysticks.TryGetValue(deviceGuid, out var joystick))
            {
                var state = statePool.Get();
                try
                {
                    joystick.GetCurrentState(ref state);
                    return state;
                }
                catch
                {
                    statePool.Return(state);
                    throw;
                }
            }
            throw new KeyNotFoundException("Joystick not found");
        }

        public void ReturnJoystickState(JoystickState state)
        {
            statePool.Return(state);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    cleanupTimer.Dispose();

                    foreach (var joystick in joysticks.Values)
                    {
                        try
                        {
                            joystick.Unacquire();
                            joystick.Dispose();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error disposing joystick: {ex.Message}");
                        }
                    }
                    joysticks.Clear();

                    if (directInput != null)
                    {
                        directInput.Dispose();
                        directInput = null;
                    }
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DInputHandler()
        {
            Dispose(false);
        }
    }

    // Pool d'objets pour réutiliser les instances de JoystickState
    public class ObjectPool<T>(Func<T> generator, int maxPoolSize) where T : class, new()
    {
        private readonly ConcurrentBag<T> objects = [];
        private readonly Func<T> objectGenerator = generator;
        private readonly int maxSize = maxPoolSize;

        public T Get()
        {
            if (objects.TryTake(out T? item))
            {
                return item;
            }
            return objectGenerator();
        }

        public void Return(T item)
        {
            if (objects.Count < maxSize)
            {
                objects.Add(item);
            }
        }
    }
}