export default function App() {
  return (
    <div style={{ maxWidth: 560, margin: '3rem auto', padding: '0 1rem' }}>
      <h1 style={{ fontSize: '1.35rem', fontWeight: 700 }}>Online zakazivanje</h1>
      <p style={{ color: '#555' }}>
        Otvorite link koji vam je salon poslao — adresa izgleda kao{' '}
        <code style={{ background: '#eee', padding: '2px 6px', borderRadius: 4 }}>
          /ime-salona
        </code>
        .
      </p>
    </div>
  );
}
