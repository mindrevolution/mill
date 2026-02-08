import { defineConfig } from 'astro/config';
import tailwind from '@astrojs/tailwind';
import mdx from '@astrojs/mdx';

export default defineConfig({
  site: 'https://mill.mindrevolution.com',
  integrations: [tailwind(), mdx()],
});
