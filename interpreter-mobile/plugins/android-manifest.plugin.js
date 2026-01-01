const { withAndroidManifest, AndroidConfig } = require('@expo/config-plugins');

const withNotifeeManifest = (config) => {
  return withAndroidManifest(config, async (config) => {
    const androidManifest = config.modResults;

    // Add permissions required for background services and notifications
    AndroidConfig.Permissions.addPermission(androidManifest, 'android.permission.FOREGROUND_SERVICE');
    AndroidConfig.Permissions.addPermission(androidManifest, 'android.permission.POST_NOTIFICATIONS');
    AndroidConfig.Permissions.addPermission(androidManifest, 'android.permission.WAKE_LOCK');

    return config;
  });
};

module.exports = withNotifeeManifest;
