import { defineCollection, z } from 'astro:content';

const chapters = defineCollection({
  type: 'content',
  schema: z.object({
    title: z.string(),
    number: z.number(),
    subtitle: z.string(),
    accent: z.string().default('flame'),
  }),
});

export const collections = { chapters };
