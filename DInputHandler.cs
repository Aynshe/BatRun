using System;
using SharpDX;
using SharpDX.DirectInput;
using System.Collections.Generic;

namespace BatRun
{
    public class DInputHandler : IDisposable
    {
        private DirectInput? directInput;
        private readonly List<Joystick> joysticks;
        private readonly Logger logger;
        private bool disposed;

        public DInputHandler(Logger logger)
        {
            this.logger = logger;
            directInput = new DirectInput();
            joysticks = [];
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
                        joysticks.Add(joystick);
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var joystick in joysticks)
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
}