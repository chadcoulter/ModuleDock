import fs from "node:fs";
import Ajv from "ajv";
import addFormats from "ajv-formats";

const schemaPath = ".ai/schemas/no-discovery-agent-response.schema.json";
const inputPath = process.env.AI_RESPONSE_FILE || ".ai/response.json";

const schema = JSON.parse(fs.readFileSync(schemaPath, "utf8"));
const input = JSON.parse(fs.readFileSync(inputPath, "utf8"));

const ajv = new Ajv({ allErrors: true, strict: false });
addFormats(ajv);

const validate = ajv.compile(schema);
const ok = validate(input);

if (!ok) {
  console.error("❌ AI response schema validation failed:");
  for (const err of validate.errors ?? []) {
    console.error(`- ${err.instancePath || "/"} ${err.message}`);
  }
  process.exit(1);
}

const changed = new Set(input.files_changed || []);
for (const d of input.diffs || []) {
  if (!changed.has(d.path)) {
    console.error(`❌ Diff path not present in files_changed: ${d.path}`);
    process.exit(1);
  }
}

console.log("✅ AI response schema validation passed");
