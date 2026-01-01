module.exports = ({ config }) => {
  return {
    ...config,
    plugins: [
      ...(config.plugins || []),
      "./plugins/android-manifest.plugin.js"
    ],
  };
};

