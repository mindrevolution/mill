import { defineConfig } from 'astro/config';
import tailwind from '@astrojs/tailwind';

export default defineConfig({
  site: 'https://mill.mindrevolution.com',
  integrations: [tailwind({ applyBaseStyles: false })],
  redirects: {
    '/install.sh': 'https://raw.githubusercontent.com/mindrevolution/mill/main/install.sh',
    '/install.ps1': 'https://raw.githubusercontent.com/mindrevolution/mill/main/install.ps1',
  },
});
