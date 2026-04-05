import styles from './ThinkingSphere.module.css';

function ThinkingSphere() {
    return (
        <div className={styles.sphereLoader}>
            <div className={`${styles.orb} ${styles.orb1}`}></div>
            <div className={`${styles.orb} ${styles.orb2}`}></div>
            <div className={`${styles.orb} ${styles.orb3}`}></div>
            <div className={styles.glassLayer}></div>
        </div>
    );
}

export default ThinkingSphere;
