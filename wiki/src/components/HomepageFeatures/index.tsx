import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  Svg: React.ComponentType<React.ComponentProps<'svg'>>;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Prozedural generierte Level',
    Svg: require('@site/static/img/feature_mountain.svg').default,
    description: (
      <>Jedes Level wird dynamisch erstellt und hält stets neue Herausforderungen bereit.</>
    ),
  },
  {
    title: 'Steampunk-Mechaniken',
    Svg: require('@site/static/img/feature_tree.svg').default,
    description: (
      <>Bewegliche Plattformen, rotierende Hindernisse und Dampfeffekte prägen das Spielerlebnis.</>
    ),
  },
  {
    title: 'Modulare Architektur',
    Svg: require('@site/static/img/feature_react.svg').default,
    description: (
      <>Sauber gekapselte Komponenten erleichtern die Erweiterung des Projekts.</>
    ),
  },
];

function Feature({title, Svg, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
