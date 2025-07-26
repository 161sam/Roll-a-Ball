import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Roll-a-Ball',
  tagline: 'Ein modular entwickeltes Steampunk-Spiel mit Unity 6.1',
  favicon: 'img/favicon.ico',

  future: {
    v4: true,
  },

  url: 'https://161sam.github.io',
  baseUrl: '/Roll-a-Ball/',

  organizationName: '161sam',
  projectName: 'Roll-a-Ball',

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  i18n: {
    defaultLocale: 'de',
    locales: ['de'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.ts'),
          editUrl: 'https://github.com/161sam/Roll-a-Ball/edit/main/wiki/',
        },
        blog: false,
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
  image: 'img/social-card.jpg',
    navbar: {
      title: 'Roll-a-Ball',
      logo: {
        alt: 'Roll-a-Ball Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Entwicklung',
        },
        {
          href: 'https://github.com/161sam/Roll-a-Ball',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Dokumentation',
          items: [
            {
              label: 'Entwicklungsübersicht',
              to: '/docs/intro',
            },
          ],
        },
        {
          title: 'Ressourcen',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/161sam/Roll-a-Ball',
            },
            {
              label: 'Unity',
              href: 'https://unity.com/',
            },
          ],
        },
      ],
      copyright: `© ${new Date().getFullYear()} Roll-a-Ball Projekt.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
