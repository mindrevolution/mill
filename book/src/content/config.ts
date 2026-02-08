import { defineCollection, z } from 'astro:content';

const chapters = defineCollection({
  type: 'content',
  schema: z.object({
    title: z.string(),
    chapter: z.number(),
    part: z.string(),
    partNumber: z.number(),
    description: z.string(),
  }),
});

export const collections = { chapters };
