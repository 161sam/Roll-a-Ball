// @ts-check

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  tutorialSidebar: [
    {
      type: 'doc',
      id: 'intro',
      label: 'Projektüberblick',
    },
    {
      type: 'category',
      label: 'Anleitungen',
      items: [
        'development/COMPLETE_FIX_GUIDE',
        'development/PROCEDURAL_GENERATION_SETUP',
        'development/PROCEDURAL_SYSTEM_READY',
        'development/GITHUB_PUSH_SOLUTION',
      ],
    },
    {
      type: 'category',
      label: 'Berichte',
      items: [
        'development/SOLUTION_SUMMARY',
        'development/CURRENT_REPAIR_STATUS',
        'development/UNITY_ERROR_RESOLUTION_REPORT',
        'development/FINAL_COMPLETION_REPORT',
        'development/FINAL_SUCCESS_REPORT',
        'development/FINAL_SUCCESS_COMPLETE',
        'development/STEAMPUNK_DEPLOYMENT_COMPLETE',
      ],
    },
    {
      type: 'category',
      label: 'Szenenberichte',
      collapsed: true,
      items: [
        'development/SceneReport_MASTER_ANALYSIS',
        'development/SceneReport_Level1',
        'development/SceneReport_Level2',
        'development/SceneReport_Level3',
        'development/SceneReport_GeneratedLevel',
        'development/SceneReport_Level_OSM',
        'development/SceneReport_MiniGame',
      ],
    },
  ],
};

export default sidebars;
