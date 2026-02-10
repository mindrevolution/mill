import { defineConfig } from 'astro/config';
import tailwind from '@astrojs/tailwind';

export default defineConfig({
  site: 'https://mill.mindrevolution.com',
  integrations: [tailwind({ applyBaseStyles: false })],
});
